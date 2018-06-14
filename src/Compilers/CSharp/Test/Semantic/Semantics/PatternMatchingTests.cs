// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                // (7,18): error CS8059: Feature 'binary literals' is not available in C# 6. Please use language version 7.0 or greater.
                //         int i1 = 0b001010; // binary literals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("binary literals", "7.0").WithLocation(7, 18),
                // (8,18): error CS8059: Feature 'digit separators' is not available in C# 6. Please use language version 7.0 or greater.
                //         int i2 = 23_554; // digit separators
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("digit separators", "7.0").WithLocation(8, 18),
                // (12,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int f() => 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "f").WithArguments("local functions", "7.0").WithLocation(12, 13),
                // (13,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(13, 9),
                // (13,22): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(13, 22),
                // (14,20): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         string s = o is string k ? k : null; // pattern matching
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "o is string k").WithArguments("pattern matching", "7.0").WithLocation(14, 20),
                // (12,13): warning CS8321: The local function 'f' is declared but never used
                //         int f() => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(12, 13)
                );

            // enables binary literals, digit separators, local functions, ref locals, pattern matching
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'i2' is assigned but its value is never used
                //         int i2 = 23_554; // digit separators
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i2").WithArguments("i2").WithLocation(8, 13),
                // (12,13): warning CS8321: The local function 'f' is declared but never used
                //         int f() => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(12, 13)
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
        Test<int>(""goo"");
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
expression goo is not Int32
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
        var oa = new object[] { 1, 10, 20L, 1.2, ""goo"", true, null, new X(), new Exception(""boo"") };
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
class String goo
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
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
            compilation.VerifyDiagnostics();

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
        public void Query_02()
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
        var res = from x1 in new[] { 1 is var y1 && Print(y1) ? 2 : 0}
                  select Print(x1);

        res.ToArray(); 
    }

    static bool Print(object x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput:
@"1
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yDecl = GetPatternDeclaration(tree, "y1");
            var yRef = GetReferences(tree, "y1").Single();
            VerifyModelForDeclarationPattern(model, yDecl, yRef);
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
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,30): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static bool Test1 = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "int x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 30)
                );
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
        [WorkItem(16935, "https://github.com/dotnet/roslyn/issues/16935")]
        public void FieldInitializers_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static System.Func<bool> Test1 = () => 1 is int x1 && Dummy(x1); 

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

            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,37): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static bool Test1 {get;} = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "int x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 37)
                );
        }

        [Fact]
        [WorkItem(16935, "https://github.com/dotnet/roslyn/issues/16935")]
        public void PropertyInitializers_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static System.Func<bool> Test1 {get;} = () => 1 is int x1 && Dummy(x1); 

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
    public D(object o) : base(2 is var x1 && Dummy(x1)) 
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
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);

            Assert.Equal("System.Int32", ((LocalSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl[0])).Type.ToTestDisplayString());

            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (12,36): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     public D(object o) : base(2 is var x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "var x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(12, 36),
                // (17,28): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     public D() : this(1 is int x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "int x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(17, 28)
                );
        }

        [Fact]
        [WorkItem(16935, "https://github.com/dotnet/roslyn/issues/16935")]
        public void ConstructorInitializers_02()
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
    public D(System.Func<bool> o) : base(() => 2 is int x1 && Dummy(x1)) 
    {
        System.Console.WriteLine(o());
    }

    public D() : this(() => 1 is int x1 && Dummy(x1)) 
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
    public C(System.Func<bool> b) 
    { 
        System.Console.WriteLine(b());
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"2
True
1
True");

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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
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

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
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
        public void While_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        int f = 1;
        var l = new System.Collections.Generic.List<System.Action>();

        while (Dummy(f < 3, f is var x1, x1))
        {
            l.Add(() => System.Console.WriteLine(x1));
            f++;
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"1
2
3
--
1
2
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void While_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        int f = 1;
        var l = new System.Collections.Generic.List<System.Action>();

        while (Dummy(f < 3, f is var x1, x1, l, () => System.Console.WriteLine(x1)))
        {
            f++;
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z, System.Collections.Generic.List<System.Action> l, System.Action d) 
    {
        l.Add(d);
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"1
2
3
--
1
2
3
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void While_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        int f = 1;
        var l = new System.Collections.Generic.List<System.Action>();

        while (Dummy(f < 3, f is var x1, x1, l, () => System.Console.WriteLine(x1)))
        {
            l.Add(() => System.Console.WriteLine(x1));
            f++;
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z, System.Collections.Generic.List<System.Action> l, System.Action d) 
    {
        l.Add(d);
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"1
2
3
--
1
1
2
2
3
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
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
            CompileAndVerify(compilation, expectedOutput: @"2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
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
        public void Do_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        int f = 1;
        var l = new System.Collections.Generic.List<System.Action>();

        do
        {
            ;
        }
        while (Dummy(f < 3, (f++) is var x1, x1, l, () => System.Console.WriteLine(x1)));

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z, System.Collections.Generic.List<System.Action> l, System.Action d) 
    {
        l.Add(d);
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
2
3
--
1
2
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
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
             Dummy(f, (f ? 100 : 200) is var x2, x2), Dummy(true, null, x2))
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
200
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").Single();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(2, x0Ref.Length);
            VerifyModelForDeclarationPattern(model, x0Decl, x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);
        }

        [Fact]
        public void For_02()
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
             f = false, Dummy(f, (f ? 100 : 200) is var x2, x2), Dummy(true, null, x2))
        {
            System.Console.WriteLine(x0);
            System.Console.WriteLine(x1);
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
200
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").Single();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(2, x0Ref.Length);
            VerifyModelForDeclarationPattern(model, x0Decl, x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);
        }

        [Fact]
        public void For_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        var l = new System.Collections.Generic.List<System.Action>();

        for (bool f = 1 is var x0; Dummy(x0 < 3, x0*10 is var x1, x1); x0++)
        {
            l.Add(() => System.Console.WriteLine(""{0} {1}"", x0, x1));
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z) 
    {
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"10
20
30
--
3 10
3 20
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").ToArray();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(1, x0Decl.Length);
            Assert.Equal(4, x0Ref.Length);
            VerifyModelForDeclarationPattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void For_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        var l = new System.Collections.Generic.List<System.Action>();

        for (bool f = 1 is var x0; Dummy(x0 < 3, x0*10 is var x1, x1, l, () => System.Console.WriteLine(""{0} {1}"", x0, x1)); x0++)
        {
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z, System.Collections.Generic.List<System.Action> l, System.Action d) 
    {
        l.Add(d);
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"10
20
30
--
3 10
3 20
3 30
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").ToArray();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(1, x0Decl.Length);
            Assert.Equal(4, x0Ref.Length);
            VerifyModelForDeclarationPattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void For_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        var l = new System.Collections.Generic.List<System.Action>();

        for (bool f = 1 is var x0; Dummy(x0 < 3, x0*10 is var x1, x1, l, () => System.Console.WriteLine(""{0} {1}"", x0, x1)); x0++)
        {
            l.Add(() => System.Console.WriteLine(""{0} {1}"", x0, x1));
        }

        System.Console.WriteLine(""--"");

        foreach (var d in l)
        {
            d();
        }
    }

    static bool Dummy(bool x, object y, object z, System.Collections.Generic.List<System.Action> l, System.Action d) 
    {
        l.Add(d);
        System.Console.WriteLine(z);
        return x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"10
20
30
--
3 10
3 10
3 20
3 20
3 30
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x0Decl = GetPatternDeclarations(tree, "x0").ToArray();
            var x0Ref = GetReferences(tree, "x0").ToArray();
            Assert.Equal(1, x0Decl.Length);
            Assert.Equal(5, x0Ref.Length);
            VerifyModelForDeclarationPattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
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
            CompileAndVerify(compilation, verify: Verification.Fails, expectedOutput:
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "12").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
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
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "2").VerifyDiagnostics(
                // (6,1): warning CS0164: This label has not been referenced
                // a:      Test1(2 is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(6, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "2").VerifyDiagnostics(
                // (15,1): warning CS0164: This label has not been referenced
                // a:          Test2(2 is var x1, x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(15, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
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
        Console.WriteLine(""goo"" is System.String); // true
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
            compilation.VerifyDiagnostics(
                // (11,27): warning CS0183: The given expression is always of the provided ('string') type
                //         Console.WriteLine("goo" is System.String); // true
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, @"""goo"" is System.String").WithArguments("string").WithLocation(11, 27)
                );
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
                Diagnostic(ErrorCode.ERR_ConstantExpected, "typeof(string[])").WithLocation(12, 18),
                // (10,17): warning CS0162: Unreachable code detected
                //                 Console.WriteLine("string");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(10, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 Console.WriteLine("string[]");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(13, 17)
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
                // (7,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case 1 when true:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case 1 when true:").WithArguments("pattern matching", "7.0").WithLocation(7, 13)
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
            var compilation = CreateCompilation(text,
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
            var compilation = CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
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

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().First();

            Assert.Equal("o.Equals is()", node.ToString());

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o.Equals is()')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'o.Equals is()')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'o.Equals')
            Children(1):
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o')
  Pattern: 
    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '()')
      Value: 
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
          Children(0)
");
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

        [Fact, WorkItem(15956, "https://github.com/dotnet/roslyn/issues/15956")]
        public void ThrowExpressionWithNullableDecimal()
        {
            var source = @"
using System;
public class ITest
{
    public decimal Test() => 1m;
}

public class TestClass
{
    public void Test(ITest test)
    {
        var result = test?.Test() ?? throw new Exception();
    }
}";
            // DEBUG
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics();

            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("TestClass.Test", @"{
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (decimal V_0, //result
                    decimal V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  brtrue.s   IL_000a
    IL_0004:  newobj     ""System.Exception..ctor()""
    IL_0009:  throw
    IL_000a:  ldarg.1
    IL_000b:  call       ""decimal ITest.Test()""
    IL_0010:  stloc.0
    IL_0011:  ret
}");

            // RELEASE
            compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics();

            verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("TestClass.Test", @"{
    // Code size       17 (0x11)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  call       ""decimal ITest.Test()""
    IL_000f:  pop
    IL_0010:  ret
}");
        }

        [Fact, WorkItem(15956, "https://github.com/dotnet/roslyn/issues/15956")]
        public void ThrowExpressionWithNullableDateTime()
        {
            var source = @"
using System;
public class ITest
{
    public DateTime Test() => new DateTime(2008, 5, 1, 8, 30, 52);
}

public class TestClass
{
    public void Test(ITest test)
    {
        var result = test?.Test() ?? throw new Exception();
    }
}";
            // DEBUG
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics();

            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("TestClass.Test", @"{
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (System.DateTime V_0, //result
                    System.DateTime V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  brtrue.s   IL_000a
    IL_0004:  newobj     ""System.Exception..ctor()""
    IL_0009:  throw
    IL_000a:  ldarg.1
    IL_000b:  call       ""System.DateTime ITest.Test()""
    IL_0010:  stloc.0
    IL_0011:  ret
}");

            
            // RELEASE
            compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics();

            verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("TestClass.Test", @"{
    // Code size       17 (0x11)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0009
    IL_0003:  newobj     ""System.Exception..ctor()""
    IL_0008:  throw
    IL_0009:  ldarg.1
    IL_000a:  call       ""System.DateTime ITest.Test()""
    IL_000f:  pop
    IL_0010:  ret
}");
    
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
        foreach (var s in new[] { ""0123"", ""goo"" })
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
goo throws");
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
                var used = (await Goo(i))?.ToString() ?? throw await Bar(i);
            }
            catch (Exception ex)
            {
                Console.WriteLine(""thrown "" + ex.Message);
            }
        }
    }
    static async Task<object> Goo(int i)
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
            var compilation = CreateEmptyCompilation(source, options: TestOptions.DebugExe,
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
                // (15,27): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         Console.WriteLine(3 is One + 2); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "3 is One + 2").WithArguments("pattern matching", "7.0").WithLocation(15, 27),
                // (16,27): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         Console.WriteLine(One + 2 is 3); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "One + 2 is 3").WithArguments("pattern matching", "7.0").WithLocation(16, 27)
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

        [Fact, WorkItem(14636, "https://github.com/dotnet/roslyn/issues/14636")]
        public void NameofPattern()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        M(""a"");
        M(""b"");
        M(null);
        M(new nameof());
    }
    public static void M(object a)
    {
        Console.WriteLine(a is nameof(a));
        Console.WriteLine(a is nameof);
    }
}
class nameof { }
";
            var expectedOutput =
@"True
False
False
False
False
False
False
True";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(14825, "https://github.com/dotnet/roslyn/issues/14825")]
        public void PatternVarDeclaredInReceiverUsedInArgument()
        {
            var source =
@"using System.Linq;

public class C
{
    public string[] Goo2(out string x) { x = """"; return null; }
    public string[] Goo3(bool b) { return null; }

    public string[] Goo5(string u) { return null; }
    
    public void Test()
    {
        var t1 = Goo2(out var x1).Concat(Goo5(x1));
        var t2 = Goo3(t1 is var x2).Concat(Goo5(x2.First()));
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.String>", model.GetTypeInfo(x2Ref).Type.ToTestDisplayString());
        }

        [Fact]
        public void DiscardInPattern()
        {
            var source =
@"
using static System.Console;
public class C
{
    public static void Main()
    {
        int i = 3;
        Write($""is int _: {i is int _}, "");
        Write($""is var _: {i is var _}, "");
        switch (3)
        {
            case int _:
                Write(""case int _, "");
                break;
        }
        switch (3L)
        {
            case var _:
                Write(""case var _"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "is int _: True, is var _: True, case int _, case var _");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var declaration1 = (DeclarationPatternSyntax)discard1.Parent;
            Assert.Equal("int _", declaration1.ToString());
            Assert.Null(model.GetTypeInfo(declaration1).Type);
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration1.Type).Type.ToTestDisplayString());

            var discard2 = GetDiscardDesignations(tree).Skip(1).First();
            Assert.Null(model.GetDeclaredSymbol(discard2));
            Assert.Null(model.GetSymbolInfo(discard2).Symbol);
            var declaration2 = (DeclarationPatternSyntax)discard2.Parent;
            Assert.Equal("var _", declaration2.ToString());
            Assert.Null(model.GetTypeInfo(declaration2).Type);
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration2.Type).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(declaration2).Symbol);

            var discard3 = GetDiscardDesignations(tree).Skip(2).First();
            Assert.Null(model.GetDeclaredSymbol(discard3));
            var declaration3 = (DeclarationPatternSyntax)discard3.Parent;
            Assert.Equal("int _", declaration3.ToString());
            Assert.Null(model.GetTypeInfo(declaration3).Type);
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration3.Type).Type.ToTestDisplayString());

            var discard4 = GetDiscardDesignations(tree).Skip(3).First();
            Assert.Null(model.GetDeclaredSymbol(discard4));
            var declaration4 = (DeclarationPatternSyntax)discard4.Parent;
            Assert.Equal("var _", declaration4.ToString());
            Assert.Null(model.GetTypeInfo(declaration4).Type);
            Assert.Equal("System.Int64", model.GetTypeInfo(declaration4.Type).Type.ToTestDisplayString());
        }

        [Fact]
        public void ShortDiscardInPattern()
        {
            var source =
@"
using static System.Console;
public class C
{
    public static void Main()
    {
        int i = 3;
        Write($""is _: {i is _}, "");
        switch (3)
        {
            case _:
                Write(""case _"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (8,29): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         Write($"is _: {i is _}, ");
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(8, 29),
                // (11,18): error CS0103: The name '_' does not exist in the current context
                //             case _:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 Write("case _");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Write").WithLocation(12, 17)
                );
        }

        [Fact]
        public void UnderscoreInPattern2()
        {
            var source =
@"
using static System.Console;
public class C
{
    public static void Main()
    {
        int i = 3;
        int _ = 4;
        Write($""is _: {i is _}, "");
        switch (3)
        {
            case _:
                Write(""case _"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (9,29): error CS0150: A constant value is expected
                //         Write($"is _: {i is _}, ");
                Diagnostic(ErrorCode.ERR_ConstantExpected, "_").WithLocation(9, 29),
                // (12,18): error CS0150: A constant value is expected
                //             case _:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "_").WithLocation(12, 18),
                // (13,17): warning CS0162: Unreachable code detected
                //                 Write("case _");
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Write").WithLocation(13, 17)
                );
        }

        [Fact]
        public void UnderscoreInPattern()
        {
            var source =
@"
using static System.Console;
public class C
{
    public static void Main()
    {
        int i = 3;
        if (i is int _) { Write(_); }
        if (i is var _) { Write(_); }
        switch (3)
        {
            case int _:
                Write(_);
                break;
        }
        switch (3)
        {
            case var _:
                Write(_);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,33): error CS0103: The name '_' does not exist in the current context
                //         if (i is int _) { Write(_); }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(8, 33),
                // (9,33): error CS0103: The name '_' does not exist in the current context
                //         if (i is var _) { Write(_); }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(9, 33),
                // (13,23): error CS0103: The name '_' does not exist in the current context
                //                 Write(_);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(13, 23),
                // (19,23): error CS0103: The name '_' does not exist in the current context
                //                 Write(_);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(19, 23)
                );
        }

        [Fact]
        public void PointerTypeInPattern()
        {
            // pointer types are not supported in patterns. Therefore an attempt to use
            // a pointer type will be interpreted by the parser as a multiplication
            // (i.e. an expression that is a constant pattern rather than a declaration
            // pattern)
            var source =
@"
public class var {}
unsafe public class Typ
{
    public static void Main(int* a, var* c, Typ* e)
    {
        {
            if (a is int* b) {}
            if (c is var* d) {}
            if (e is Typ* f) {}
        }
        {
            switch (a) { case int* b: break; }
            switch (c) { case var* d: break; }
            switch (e) { case Typ* f: break; }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            compilation.VerifyDiagnostics(
                // (8,22): error CS1525: Invalid expression term 'int'
                //             if (a is int* b) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(8, 22),
                // (13,31): error CS1525: Invalid expression term 'int'
                //             switch (a) { case int* b: break; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(13, 31),
                // (5,37): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('var')
                //     public static void Main(int* a, var* c, Typ* e)
                Diagnostic(ErrorCode.ERR_ManagedAddr, "var*").WithArguments("var").WithLocation(5, 37),
                // (5,45): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('Typ')
                //     public static void Main(int* a, var* c, Typ* e)
                Diagnostic(ErrorCode.ERR_ManagedAddr, "Typ*").WithArguments("Typ").WithLocation(5, 45),
                // (8,27): error CS0103: The name 'b' does not exist in the current context
                //             if (a is int* b) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(8, 27),
                // (9,22): error CS0119: 'var' is a type, which is not valid in the given context
                //             if (c is var* d) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "var").WithArguments("var", "type").WithLocation(9, 22),
                // (9,27): error CS0103: The name 'd' does not exist in the current context
                //             if (c is var* d) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(9, 27),
                // (10,22): error CS0119: 'Typ' is a type, which is not valid in the given context
                //             if (e is Typ* f) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Typ").WithArguments("Typ", "type").WithLocation(10, 22),
                // (10,27): error CS0103: The name 'f' does not exist in the current context
                //             if (e is Typ* f) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(10, 27),
                // (13,36): error CS0103: The name 'b' does not exist in the current context
                //             switch (a) { case int* b: break; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(13, 36),
                // (14,31): error CS0119: 'var' is a type, which is not valid in the given context
                //             switch (c) { case var* d: break; }
                Diagnostic(ErrorCode.ERR_BadSKunknown, "var").WithArguments("var", "type").WithLocation(14, 31),
                // (14,36): error CS0103: The name 'd' does not exist in the current context
                //             switch (c) { case var* d: break; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(14, 36),
                // (15,31): error CS0119: 'Typ' is a type, which is not valid in the given context
                //             switch (e) { case Typ* f: break; }
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Typ").WithArguments("Typ", "type").WithLocation(15, 31),
                // (15,36): error CS0103: The name 'f' does not exist in the current context
                //             switch (e) { case Typ* f: break; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(15, 36),
                // (13,39): warning CS0162: Unreachable code detected
                //             switch (a) { case int* b: break; }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(13, 39),
                // (14,39): warning CS0162: Unreachable code detected
                //             switch (c) { case var* d: break; }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(14, 39),
                // (15,39): warning CS0162: Unreachable code detected
                //             switch (e) { case Typ* f: break; }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(15, 39)
                );
        }

        [Fact]
        [WorkItem(16513, "https://github.com/dotnet/roslyn/issues/16513")]
        public void OrderOfPatternOperands()
        {
            var source = @"
using System;
class Program
{
    public static void Main(string[] args)
    {
        object c = new C();
        Console.WriteLine(c is 3);
        c = 2;
        Console.WriteLine(c is 3);
        c = 3;
        Console.WriteLine(c is 3);
    }
}
class C
{
    override public bool Equals(object other)
    {
        return other is int x;
    }
    override public int GetHashCode() => 0;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: @"False
False
True");
        }

        [Fact]
        public void MultiplyInPattern()
        {
            // pointer types are not supported in patterns. Therefore an attempt to use
            // a pointer type will be interpreted by the parser as a multiplication
            // (i.e. an expression that is a constant pattern rather than a declaration
            // pattern)
            var source =
@"
public class Program
{
    public static void Main()
    {
        const int two = 2;
        const int three = 3;
        int six = two * three;
        System.Console.WriteLine(six is two * three);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: "True");
        }

        [Fact]
        public void ColorColorConstantPattern()
        {
            var source =
@"
public class Program
{
    public static Color Color { get; }

    public static void M(object o)
    {
        System.Console.WriteLine(o is Color.Constant);
    }

    public static void Main()
    {
        M(Color.Constant);
    }
}

public class Color
{
    public const string Constant = ""abc"";
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: "True");
        }

        [Fact]
        [WorkItem(336030, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/336030")]
        public void NullOperand()
        {
            var source = @"
class C
{
    void M()
    {
        System.Console.Write(null is Missing x);
        System.Console.Write(null is Missing);
        switch(null)
        {
            case Missing:
            case Missing y:
                break;
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,30): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         System.Console.Write(null is Missing x);
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(6, 30),
                // (6,38): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Console.Write(null is Missing x);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(6, 38),
                // (7,38): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Console.Write(null is Missing);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(7, 38),
                // (8,16): error CS8119: The switch expression must be a value; found '<null>'.
                //         switch(null)
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "null").WithArguments("<null>").WithLocation(8, 16),
                // (10,18): error CS0103: The name 'Missing' does not exist in the current context
                //             case Missing:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing").WithLocation(10, 18),
                // (11,18): error CS0246: The type or namespace name 'Missing' could not be found (are you missing a using directive or an assembly reference?)
                //             case Missing y:
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        [WorkItem(336030, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=336030")]
        [WorkItem(294570, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=294570")]
        public void Fuzz46()
        {
            var program = @"
public class Program46
{
    public static void Main(string[] args)
    {
        switch ((() => 1))
        {
            case int x4:
            case string x9:
            case M:
            case ((int)M()):
                break;
        }
    }
    private static object M() => null;
}";
            CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (6,17): error CS8119: The switch expression must be a value; found 'lambda expression'.
                //         switch ((() => 1))
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "(() => 1)").WithArguments("lambda expression").WithLocation(6, 17),
                // (10,18): error CS0150: A constant value is expected
                //             case M:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M").WithLocation(10, 18),
                // (11,18): error CS0150: A constant value is expected
                //             case ((int)M()):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "((int)M())").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        [WorkItem(363714, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=363714")]
        public void Fuzz46b()
        {
            var program = @"
public class Program46
{
    public static void Main(string[] args)
    {
        switch ((() => 1))
        {
            case M:
                break;
        }
    }
    private static object M() => null;
}";
            CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (6,17): error CS8119: The switch expression must be a value; found lambda expression.
                //         switch ((() => 1))
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "(() => 1)").WithArguments("lambda expression").WithLocation(6, 17),
                // (8,18): error CS0150: A constant value is expected
                //             case M:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M").WithLocation(8, 18),
                // (9,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(9, 17)
                );
        }

        [Fact]
        [WorkItem(336030, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=336030")]
        public void Fuzz401()
        {
            var program = @"
public class Program401
{
    public static void Main(string[] args)
    {
        if (null is M) {}
    }
    private static object M() => null;
}";
            CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is M) {}
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithArguments("<null>").WithLocation(6, 13),
                // (6,21): error CS0150: A constant value is expected
                //         if (null is M) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M").WithLocation(6, 21)
                );
        }

        [Fact]
        [WorkItem(364165, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=364165")]
        [WorkItem(16296, "https://github.com/dotnet/roslyn/issues/16296")]
        public void Fuzz1717()
        {
            var program = @"
public class Program1717
{
    public static void Main(string[] args)
    {
        switch (default(int?))
        {
            case 2:
                break;
            case double.NaN:
                break;
            case var x9:
            case string _:
                break;
        }
    }
    private static object M() => null;
}";
            CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (10,18): error CS0266: Cannot implicitly convert type 'double' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //             case double.NaN:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.NaN").WithArguments("double", "int?").WithLocation(10, 18),
                // (13,18): error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'string'.
                //             case string _:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("int?", "string").WithLocation(13, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact, WorkItem(16559, "https://github.com/dotnet/roslyn/issues/16559")]
        public void CasePatternVariableUsedInCaseExpression()
        {
            var program = @"
public class Program5815
{
    public static void Main(object o)
    {
        switch (o)
        {
            case Color Color:
            case Color? Color2:
                break;
        }
    }
    private static object M() => null;
}";
            var compilation = CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (9,32): error CS1525: Invalid expression term 'break'
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("break").WithLocation(9, 32),
                // (9,32): error CS1003: Syntax error, ':' expected
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "break").WithLocation(9, 32),
                // (8,18): error CS0118: 'Color' is a variable but is used like a type
                //             case Color Color:
                Diagnostic(ErrorCode.ERR_BadSKknown, "Color").WithArguments("Color", "variable", "type").WithLocation(8, 18),
                // (9,25): error CS0103: The name 'Color2' does not exist in the current context
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Color2").WithArguments("Color2").WithLocation(9, 25)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var colorDecl = GetPatternDeclarations(tree, "Color").ToArray();
            var colorRef = GetReferences(tree, "Color").ToArray();
            Assert.Equal(1, colorDecl.Length);
            Assert.Equal(2, colorRef.Length);
            Assert.Null(model.GetSymbolInfo(colorRef[0]).Symbol);
            VerifyModelForDeclarationPattern(model, colorDecl[0], colorRef[1]);
        }

        [Fact, WorkItem(16559, "https://github.com/dotnet/roslyn/issues/16559")]
        public void Fuzz5815()
        {
            var program = @"
public class Program5815
{
    public static void Main(string[] args)
    {
        switch ((int)M())
        {
            case var x3:
            case true ? x3 : 4:
                break;
        }
    }
    private static object M() => null;
}";
            var compilation = CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (9,18): error CS0150: A constant value is expected
                //             case true ? x3 : 4:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "true ? x3 : 4").WithLocation(9, 18)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").ToArray();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(1, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl[0], x3Ref);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/16721")]
        public void Fuzz()
        {
            const int numTests = 1000000;
            int dt = (int)Math.Abs(DateTime.Now.Ticks % 1000000000);
            for (int i = 1; i < numTests; i++)
            {
                PatternMatchingFuzz(i + dt);
            }
        }

        private static void PatternMatchingFuzz(int dt)
        {
            Random r = new Random(dt);

            // generate a pattern-matching switch randomly from templates
            string[] expressions = new[]
            {
                "M",              // a method group
                "(() => 1)",      // a lambda expression
                "1",              // a constant
                "2",              // a constant
                "null",           // the null constant
                "default(int?)",  // a null constant of type int?
                "((int?)1)",      // a constant of type int?
                "M()",            // a method invocation
                "double.NaN",     // a scary constant
                "1.1",            // a double constant
                "NotFound"        // an unbindable expression
            };
            string Expression()
            {
                int index = r.Next(expressions.Length + 1) - 1;
                return (index < 0) ? $"(({Type()})M())" : expressions[index];
            }
            string[] types = new[]
            {
                "object",
                "var",
                "int",
                "int?",
                "double",
                "string",
                "NotFound"
            };
            string Type() => types[r.Next(types.Length)];
            string Pattern()
            {
                switch (r.Next(3))
                {
                    case 0:
                        return Expression(); // a "constant" pattern
                    case 1:
                        return Type() + " x" + r.Next(10);
                    case 2:
                        return Type() + " _";
                    default:
                        throw null;
                }
            }
            string body = @"
public class Program{0}
{{
    public static void Main(string[] args)
    {{
        {1}
    }}
    private static object M() => null;
}}";
            var statement = new StringBuilder();
            switch (r.Next(2))
            {
                case 0:
                    // test the "is-pattern" expression
                    statement.Append($"if ({Expression()} is {Pattern()}) {{}}");
                    break;
                case 1:
                    // test the pattern switch statement
                    statement.AppendLine($"switch ({Expression()})");
                    statement.AppendLine("{");
                    var nCases = r.Next(5);
                    for (int i = 1; i <= nCases; i++)
                    {
                        statement.AppendLine($"    case {Pattern()}:");
                        if (i == nCases || r.Next(2) == 0)
                        {
                            statement.AppendLine($"        break;");
                        }
                    }
                    statement.AppendLine("}");
                    break;
                default:
                    throw null;
            }
            var program = string.Format(body, dt, statement);
            CreateCompilationWithMscorlib45(program).GetDiagnostics();
        }

        [Fact, WorkItem(16671, "https://github.com/dotnet/roslyn/issues/16671")]
        public void TypeParameterSubsumption01()
        {
            var program = @"
using System;
public class Program
{
    public static void Main(string[] args)
    {
        PatternMatching<Base, Derived>(new Base());
        PatternMatching<Base, Derived>(new Derived());
        PatternMatching<Base, Derived>(null);
        PatternMatching<object, int>(new object());
        PatternMatching<object, int>(2);
        PatternMatching<object, int>(null);
        PatternMatching<object, int?>(new object());
        PatternMatching<object, int?>(2);
        PatternMatching<object, int?>(null);
    }
    static void PatternMatching<TBase, TDerived>(TBase o) where TDerived : TBase
    {
        switch (o)
        {
            case TDerived td:
                Console.WriteLine(nameof(TDerived));
                break;
            case TBase tb:
                Console.WriteLine(nameof(TBase));
                break;
            default:
                Console.WriteLine(""Neither"");
                break;
        }
    }
}
class Base
{
}
class Derived : Base
{
}
";
            var compilation = CreateCompilationWithMscorlib45(program, options: TestOptions.DebugExe).VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"TBase
TDerived
Neither
TBase
TDerived
Neither
TBase
TDerived
Neither");
        }

        [Fact, WorkItem(16671, "https://github.com/dotnet/roslyn/issues/16671")]
        public void TypeParameterSubsumption02()
        {
            var program = @"
using System;
public class Program
{
    static void PatternMatching<TBase, TDerived>(TBase o) where TDerived : TBase
    {
        switch (o)
        {
            case TBase tb:
                Console.WriteLine(nameof(TBase));
                break;
            case TDerived td:
                Console.WriteLine(nameof(TDerived));
                break;
            default:
                Console.WriteLine(""Neither"");
                break;
        }
    }
}
class Base
{
}
class Derived : Base
{
}
";
            var compilation = CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (12,18): error CS8120: The switch case has already been handled by a previous case.
                //             case TDerived td:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "TDerived td").WithLocation(12, 18),
                // (13,17): warning CS0162: Unreachable code detected
                //                 Console.WriteLine(nameof(TDerived));
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Console").WithLocation(13, 17)
                );
        }

        [Fact, WorkItem(16688, "https://github.com/dotnet/roslyn/issues/16688")]
        public void TypeParameterSubsumption03()
        {
            var program = @"
using System.Collections.Generic;
public class Program
{
    private static void Pattern<T>(T thing) where T : class
    {
        switch (thing)
        {
            case T tThing:
                break;
            case IEnumerable<object> s:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(program).VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case IEnumerable<object> s:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "IEnumerable<object> s").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact, WorkItem(16696, "https://github.com/dotnet/roslyn/issues/16696")]
        public void TypeParameterSubsumption04()
        {
            var program = @"
using System;
using System.Collections.Generic;
public class Program
{
    private static int Pattern1<TBase, TDerived>(object thing) where TBase : class where TDerived : TBase
    {
        switch (thing)
        {
            case IEnumerable<TBase> sequence:
                return 1;
            // IEnumerable<TBase> does not subsume IEnumerable<TDerived> because TDerived may be a value type.
            case IEnumerable<TDerived> derivedSequence:
                return 2;
            default:
                return 3;
        }
    }
    private static int Pattern2<TBase, TDerived>(object thing) where TBase : class where TDerived : TBase
    {
        switch (thing)
        {
            case IEnumerable<object> s:
                return 1;
            // IEnumerable<object> does not subsume IEnumerable<TDerived> because TDerived may be a value type.
            case IEnumerable<TDerived> derivedSequence:
                return 2;
            default:
                return 3;
        }
    }
    public static void Main(string[] args)
    {
        Console.WriteLine(Pattern1<object, int>(new List<object>()));
        Console.WriteLine(Pattern1<object, int>(new List<int>()));
        Console.WriteLine(Pattern1<object, int>(null));
        Console.WriteLine(Pattern2<object, int>(new List<object>()));
        Console.WriteLine(Pattern2<object, int>(new List<int>()));
        Console.WriteLine(Pattern2<object, int>(null));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(program, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"1
2
3
1
2
3");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void TypeParameterSubsumption05()
        {
            var program = @"
public class Program
{
    static void M<T, U>(T t, U u) where T : U
    {
        switch(""test"")
        {
            case U uu:
                break;
            case T tt: // Produces a diagnostic about subsumption/unreachability
                break;
        }
    }
}
";
            CreateCompilation(program, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (8,18): error CS8314: An expression of type 'string' cannot be handled by a pattern of type 'U' in C# 7.0. Please use language version 7.1 or greater.
                //             case U uu:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "U").WithArguments("string", "U", "7.0", "7.1").WithLocation(8, 18),
                // (10,18): error CS8314: An expression of type 'string' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T tt: // Produces a diagnostic about subsumption/unreachability
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("string", "T", "7.0", "7.1").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
            CreateCompilation(program, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7_1).VerifyDiagnostics(
                // (10,18): error CS8120: The switch case has already been handled by a previous case.
                //             case T tt: // Produces a diagnostic about subsumption/unreachability
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "T tt").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact, WorkItem(17103, "https://github.com/dotnet/roslyn/issues/17103")]
        public void IsConstantPatternConversion_Positive()
        {
            var source =
@"using System;
public class Program
{
    public static void Main()
    {
        {
            byte b = 12;
            Console.WriteLine(b is 12); // True
            Console.WriteLine(b is 13); // False
            Console.WriteLine(b is (int)12L); // True
            Console.WriteLine(b is (int)13L); // False
        }
        bool Is42(byte b) => b is 42;
        Console.WriteLine(Is42(42));
        Console.WriteLine(Is42(43));
        Console.WriteLine(Is42((int)42L));
        Console.WriteLine(Is42((int)43L));
    }
}";
            var expectedOutput =
@"True
False
True
False
True
False
True
False";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(17103, "https://github.com/dotnet/roslyn/issues/17103")]
        public void IsConstantPatternConversion_Negative()
        {
            var source =
@"using System;
public class Program
{
    public static void Main()
    {
        byte b = 12;
        Console.WriteLine(b is 12L);
        Console.WriteLine(1 is null);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,32): error CS0266: Cannot implicitly convert type 'long' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         Console.WriteLine(b is 12L);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "12L").WithArguments("long", "byte"),
                // (8,32): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         Console.WriteLine(1 is null);
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 32)
                );
        }

        [Fact]
        [WorkItem(9542, "https://github.com/dotnet/roslyn/issues/9542")]
        [WorkItem(16876, "https://github.com/dotnet/roslyn/issues/16876")]
        public void DecisionTreeCoverage_Positive()
        {
            // tests added to complete coverage of the decision tree and pattern-matching implementation
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        void M1(int i, bool b)
        {
            switch (i)
            {
                case 1 when b:
                    Console.WriteLine(""M1a""); break;
                case 1:
                    Console.WriteLine(""M1b""); break;
                case 2:
                    Console.WriteLine(""M1c""); break;
            }
        }
        M1(1, true);
        M1(1, false);
        M1(2, false);
        M1(3, false);

        void M2(object o, bool b)
        {
            switch (o)
            {
                case null:
                    Console.WriteLine(""M2a""); break;
                case var _ when b:
                    Console.WriteLine(""M2b""); break;
                case 1:
                    Console.WriteLine(""M2c""); break;
            }
        }
        M2(null, true);
        M2(1, true);
        M2(1, false);

        void M3(bool? b1, bool b2)
        {
            switch (b1)
            {
                case null:
                    Console.WriteLine(""M3a""); break;
                case var _ when b2:
                    Console.WriteLine(""M3b""); break;
                case true:
                    Console.WriteLine(""M3c""); break;
                case false:
                    Console.WriteLine(""M3d""); break;
            }
        }
        M3(null, true);
        M3(true, true);
        M3(true, false);
        M3(false, false);

        void M4(object o, bool b)
        {
            switch (o)
            {
                case var _ when b:
                    Console.WriteLine(""M4a""); break;
                case int i:
                    Console.WriteLine(""M4b""); break;
            }
        }
        M4(1, true);
        M4(1, false);

        void M5(int? i, bool b)
        {
            switch (i)
            {
                case var _ when b:
                    Console.WriteLine(""M5a""); break;
                case null:
                    Console.WriteLine(""M5b""); break;
                case int q:
                    Console.WriteLine(""M5c""); break;
            }
        }
        M5(1, true);
        M5(null, false);
        M5(1, false);

        void M6(object o, bool b)
        {
            switch (o)
            {
                case var _ when b:
                    Console.WriteLine(""M6a""); break;
                case object q:
                    Console.WriteLine(""M6b""); break;
                case null:
                    Console.WriteLine(""M6c""); break;
            }
        }
        M6(null, true);
        M6(1, false);
        M6(null, false);

        void M7(object o, bool b)
        {
            switch (o)
            {
                case null when b:
                    Console.WriteLine(""M7a""); break;
                case object q:
                    Console.WriteLine(""M7b""); break;
                case null:
                    Console.WriteLine(""M7c""); break;
            }
        }
        M7(null, true);
        M7(1, false);
        M7(null, false);

        void M8(object o)
        {
            switch (o)
            {
                case null when false:
                    throw null;
                case null:
                    Console.WriteLine(""M8a""); break;
            }
        }
        M8(null);

        void M9(object o, bool b1, bool b2)
        {
            switch (o)
            {
                case var _ when b1:
                    Console.WriteLine(""M9a""); break;
                case var _ when b2:
                    Console.WriteLine(""M9b""); break;
                case var _:
                    Console.WriteLine(""M9c""); break;
            }
        }
        M9(1, true, false);
        M9(1, false, true);
        M9(1, false, false);

        void M10(bool b)
        {
            const string nullString = null;
            switch (nullString)
            {
                case null when b:
                    Console.WriteLine(""M10a""); break;
                case var _:
                    Console.WriteLine(""M10b""); break;
            }
        }
        M10(true);
        M10(false);

        void M11()
        {
            const string s = """";
            switch (s)
            {
                case string _:
                    Console.WriteLine(""M11a""); break;
            }
        }
        M11();

        void M12(bool cond)
        {
            const string s = """";
            switch (s)
            {
                case string _ when cond:
                    Console.WriteLine(""M12a""); break;
                case var _:
                    Console.WriteLine(""M12b""); break;
            }
        }
        M12(true);
        M12(false);

        void M13(bool cond)
        {
            string s = """";
            switch (s)
            {
                case string _ when cond:
                    Console.WriteLine(""M13a""); break;
                case string _:
                    Console.WriteLine(""M13b""); break;
            }
        }
        M13(true);
        M13(false);

        void M14()
        {
            const string s = """";
            switch (s)
            {
                case s:
                    Console.WriteLine(""M14a""); break;
            }
        }
        M14();

        void M15()
        {
            const int i = 3;
            switch (i)
            {
                case 3:
                case 4:
                case 5:
                    Console.WriteLine(""M15a""); break;
            }
        }
        M15();

    }
}";
            var expectedOutput =
@"M1a
M1b
M1c
M2a
M2b
M2c
M3a
M3b
M3c
M3d
M4a
M4b
M5a
M5b
M5c
M6a
M6b
M6c
M7a
M7b
M7c
M8a
M9a
M9b
M9c
M10a
M10b
M11a
M12a
M12b
M13a
M13b
M14a
M15a
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(9542, "https://github.com/dotnet/roslyn/issues/9542")]
        public void DecisionTreeCoverage_BadEquals()
        {
            // tests added to complete coverage of the decision tree and pattern-matching implementation
            var source =
@"public class X
{
    static void M1(float o)
    {
        switch (o)
        {
            case 1.1F: break;
        }
    }
}
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Char { }
    public class String { }
}
namespace System
{
    public struct Single
    {
        private Single m_value;
        public /*note bad return type*/ void Equals(Single other) { m_value = m_value + 1; }
    }
}
";
            var compilation = CreateEmptyCompilation(source);
            compilation.VerifyDiagnostics(
                );
            compilation.GetEmitDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Warning).Verify(
                // (5,9): error CS0407: 'void float.Equals(float)' has the wrong return type
                //         switch (o)
                Diagnostic(ErrorCode.ERR_BadRetType, @"switch (o)
        {
            case 1.1F: break;
        }").WithArguments("float.Equals(float)", "void").WithLocation(5, 9)
                );
        }

        [Fact]
        [WorkItem(9542, "https://github.com/dotnet/roslyn/issues/9542")]
        public void DecisionTreeCoverage_DuplicateDefault()
        {
            // tests added to complete coverage of the decision tree and pattern-matching implementation
            var source =
@"public class X
{
    static void M1(object o)
    {
        switch (o)
        {
            case int x:
            default:
            default:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyDiagnostics(
                // (9,13): error CS0152: The switch statement contains multiple cases with the label value 'default'
                //             default:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default").WithLocation(9, 13)
                );
        }

        [Fact]
        [WorkItem(9542, "https://github.com/dotnet/roslyn/issues/9542")]
        public void DecisionTreeCoverage_Negative()
        {
            // tests added to complete coverage of the decision tree and pattern-matching implementation
            var source =
@"public class X
{
    static void M1(object o)
    {
        switch (o)
        {
            case 1:
            case int _:
            case 2:
                break;
        }
    }
    static void M2(object o)
    {
        switch (o)
        {
            case 1:
            case int _:
            case int _:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.VerifyDiagnostics(
                // (9,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 2:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 2:").WithLocation(9, 13),
                // (19,18): error CS8120: The switch case has already been handled by a previous case.
                //             case int _:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "int _").WithLocation(19, 18)
                );
        }

        [Fact]
        [WorkItem(17089, "https://github.com/dotnet/roslyn/issues/17089")]
        public void Dynamic_01()
        {
            var source =
@"using System;
public class X
{
    static void M1(dynamic d)
    {
        if (d is 1)
        {
            Console.Write('r');
        }
        else if (d is int i)
        {
            Console.Write('o');
        }
        else if (d is var z)
        {
            long l = z;
            Console.Write('s');
        }
    }
    static void M2(dynamic d)
    {
        switch (d)
        {
            case 1:
                Console.Write('l');
                break;
            case int i:
                Console.Write('y');
                break;
            case var z:
                long l = z;
                Console.Write('n');
                break;
        }
    }
    public static void Main(string[] args)
    {
        M1(1);
        M1(2);
        M1(3L);
        M2(1);
        M2(2);
        M2(3L);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe);
            var comp = CompileAndVerify(compilation, expectedOutput: "roslyn");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void OpenTypeMatch_01()
        {
            var source =
@"using System;
public class Base { }
public class Derived : Base { }
public class Program
{
    public static void Main(string[] args)
    {
        M(new Derived());
        M(new Base());
    }
    public static void M<T>(T x) where T: Base
    {
        Console.Write(x is Derived b0);
        switch (x)
        {
            case Derived b1:
                Console.Write(1);
                break;
            default:
                Console.Write(0);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Derived' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Derived b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived").WithArguments("T", "Derived", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Derived' in C# 7.0. Please use language version 7.1 or greater.
                //             case Derived b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived").WithArguments("T", "Derived", "7.0", "7.1").WithLocation(16, 18)
                );
            compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True1False0");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void OpenTypeMatch_02()
        {
            var source =
@"using System;
public class Base { }
public class Derived : Base { }
public class Program
{
    public static void Main(string[] args)
    {
        M<Derived>(new Derived());
        M<Derived>(new Base());
    }
    public static void M<T>(Base x)
    {
        Console.Write(x is T b0);
        switch (x)
        {
            case T b1:
                Console.Write(1);
                break;
            default:
                Console.Write(0);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS9003: An expression of type 'Base' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is T b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("Base", "T", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS9003: An expression of type 'Base' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("Base", "T", "7.0", "7.1")
                );
            compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True1False0");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void OpenTypeMatch_03()
        {
            var source =
@"using System;
public class Base { }
public class Derived<T> : Base { }
public class Program
{
    public static void Main(string[] args)
    {
        M<Base>(new Derived<Base>());
        M<Base>(new Base());
    }
    public static void M<T>(T x) where T: Base
    {
        Console.Write(x is Derived<T> b0);
        switch (x)
        {
            case Derived<T> b1:
                Console.Write(1);
                break;
            default:
                Console.Write(0);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Derived<T>' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Derived<T> b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived<T>").WithArguments("T", "Derived<T>", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Derived<T>' in C# 7.0. Please use language version 7.1 or greater.
                //             case Derived<T> b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived<T>").WithArguments("T", "Derived<T>", "7.0", "7.1").WithLocation(16, 18)
                );
            compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True1False0");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void OpenTypeMatch_04()
        {
            var source =
@"using System;
public class Base { }
class Container<T>
{
    public class Derived : Base { }
}
public class Program
{
    public static void Main(string[] args)
    {
        M<Base>(new Container<Base>.Derived());
        M<Base>(new Base());
    }
    public static void M<T>(T x) where T: Base
    {
        Console.Write(x is Container<T>.Derived b0);
        switch (x)
        {
            case Container<T>.Derived b1:
                Console.Write(1);
                break;
            default:
                Console.Write(0);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (16,28): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Container<T>.Derived' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Container<T>.Derived b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived").WithArguments("T", "Container<T>.Derived", "7.0", "7.1").WithLocation(16, 28),
                // (19,18): error CS9003: An expression of type 'T' cannot be handled by a pattern of type 'Container<T>.Derived' in C# 7.0. Please use language version 7.1 or greater.
                //             case Container<T>.Derived b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived").WithArguments("T", "Container<T>.Derived", "7.0", "7.1").WithLocation(19, 18)
                );
            compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True1False0");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void OpenTypeMatch_05()
        {
            var source =
@"using System;
public class Base { }
class Container<T>
{
    public class Derived : Base { }
}
public class Program
{
    public static void Main(string[] args)
    {
        M<Base>(new Container<Base>.Derived[1]);
        M<Base>(new Base[1]);
    }
    public static void M<T>(T[] x) where T: Base
    {
        Console.Write(x is Container<T>.Derived[] b0);
        switch (x)
        {
            case Container<T>.Derived[] b1:
                Console.Write(1);
                break;
            default:
                Console.Write(0);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (16,28): error CS9003: An expression of type 'T[]' cannot be handled by a pattern of type 'Container<T>.Derived[]' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Container<T>.Derived[] b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived[]").WithArguments("T[]", "Container<T>.Derived[]", "7.0", "7.1").WithLocation(16, 28),
                // (19,18): error CS9003: An expression of type 'T[]' cannot be handled by a pattern of type 'Container<T>.Derived[]' in C# 7.0. Please use language version 7.1 or greater.
                //             case Container<T>.Derived[] b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived[]").WithArguments("T[]", "Container<T>.Derived[]", "7.0", "7.1").WithLocation(19, 18)
                );
            compilation = CreateCompilationWithMscorlib45(source, references: new MetadataReference[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True1False0");
        }

        [Fact, WorkItem(19151, "https://github.com/dotnet/roslyn/issues/19151")]
        public void RefutablePatterns()
        {
            var source =
@"public class Program
{
    public static void Main(string[] args)
    {
        if (null as string is string) { }
        if (null as string is string s1) { }
        const string s = null;
        if (s is string) { }
        if (s is string s2) { }
        if (""goo"" is string s3) { }
    }
    void M1(int? i)
    {
        if (i is long) { }
        if (i is long l) { }
        switch (b) { case long m: break; }
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,13): warning CS0184: The given expression is never of the provided ('string') type
                //         if (s is string) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is string").WithArguments("string").WithLocation(8, 13),
                // (9,13): warning CS0184: The given expression is never of the provided ('string') type
                //         if (s is string s2) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is string s2").WithArguments("string").WithLocation(9, 13),
                // (14,13): warning CS0184: The given expression is never of the provided ('long') type
                //         if (i is long) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "i is long").WithArguments("long").WithLocation(14, 13),
                // (15,18): error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'long'.
                //         if (i is long l) { }
                Diagnostic(ErrorCode.ERR_PatternWrongType, "long").WithArguments("int?", "long").WithLocation(15, 18),
                // (16,17): error CS0103: The name 'b' does not exist in the current context
                //         switch (b) { case long m: break; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(16, 17),
                // (16,35): warning CS0162: Unreachable code detected
                //         switch (b) { case long m: break; }
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(16, 35)
                );
        }

        [Fact, WorkItem(19038, "https://github.com/dotnet/roslyn/issues/19038")]
        public void GenericDynamicIsObject()
        {
            var program = @"
using System;
public class Program
{
    static void Main(string[] args)
    {
        M<dynamic>(new object());
        M<dynamic>(null);
        M<dynamic>(""xyzzy"");
    }
    static void M<T>(object x)
    {
        switch (x)
        {
            case T t:
                Console.Write(""T"");
                break;
            case null:
                Console.Write(""n"");
                break;
        }
    }
}
";
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"TnT");
        }

        [Fact, WorkItem(19038, "https://github.com/dotnet/roslyn/issues/19038")]
        public void MatchNullableTypeParameter()
        {
            var program = @"
using System;
public class Program
{
    static void Main(string[] args)
    {
        M<int>(1);
        M<int>(null);
        M<float>(3.14F);
    }
    static void M<T>(T? x) where T : struct
    {
        switch (x)
        {
            case T t:
                Console.Write(""T"");
                break;
            case null:
                Console.Write(""n"");
                break;
        }
    }
}
";
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"TnT");
        }

        [Fact, WorkItem(16195, "https://github.com/dotnet/roslyn/issues/16195")]
        public void MatchRecursiveGenerics()
        {
            var program =
@"using System;
class Packet { }
class Packet<U> : Packet { }
public class C {
    static void Main()
    {
        Console.Write(M<Packet>(null));
        Console.Write(M<Packet>(new Packet<Packet>()));
        Console.Write(M<Packet>(new Packet<int>()));
        Console.Write(M<Packet<int>>(new Packet<int>()));
    }
    static bool M<T>(T p) where T : Packet => p is Packet<T> p1;
}";
            CreateCompilation(program, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (12,52): error CS8314: An expression of type 'T' cannot be handled by a pattern of type 'Packet<T>' in C# 7.0. Please use language version 7.1 or greater.
                //     static bool M<T>(T p) where T : Packet => p is Packet<T> p1;
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Packet<T>").WithArguments("T", "Packet<T>", "7.0", "7.1").WithLocation(12, 52)
                );
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_1);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"FalseTrueFalseFalse");
        }

        [Fact, WorkItem(19038, "https://github.com/dotnet/roslyn/issues/19038")]
        public void MatchRestrictedTypes_Fail()
        {
            
            var program =
@"using System;
unsafe public class C {
    static bool M(TypedReference x, int* p, ref int z)
    {
        var n1 = x is TypedReference x0; // ok
        var p1 = p is int* p0;           // syntax error 1
        var r1 = z is ref int z0;        // syntax error 2

        var b1 = x is object o1;         // not allowed 1
        var b2 = p is object o2;         // not allowed 2
        var b3 = z is object o3;         // ok

        return b1 && b2 && b3;
    }
}";
            var compilation = CreateCompilation(program, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                // (6,23): error CS1525: Invalid expression term 'int'
                //         var p1 = p is int* p0;           // syntax error 1
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 23),
                // (7,23): error CS1525: Invalid expression term 'ref'
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(7, 23),
                // (7,23): error CS1002: ; expected
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(7, 23),
                // (6,28): error CS0103: The name 'p0' does not exist in the current context
                //         var p1 = p is int* p0;           // syntax error 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p0").WithArguments("p0").WithLocation(6, 28),
                // (7,31): error CS8174: A declaration of a by-reference variable must have an initializer
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "z0").WithLocation(7, 31),
                // (9,23): error CS8121: An expression of type 'TypedReference' cannot be handled by a pattern of type 'object'.
                //         var b1 = x is object o1;         // not allowed 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("System.TypedReference", "object").WithLocation(9, 23),
                // (10,23): error CS0244: Neither 'is' nor 'as' is valid on pointer types
                //         var b2 = p is object o2;         // not allowed 2
                Diagnostic(ErrorCode.ERR_PointerInAsOrIs, "object o2").WithLocation(10, 23),
                // (7,31): warning CS0168: The variable 'z0' is declared but never used
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z0").WithArguments("z0").WithLocation(7, 31)
                );
        }

        [Fact, WorkItem(19038, "https://github.com/dotnet/roslyn/issues/19038")]
        public void MatchRestrictedTypes_Success()
        {
            var program =
@"using System;
using System.Reflection;
unsafe public class C {
    public int Value;

    static void Main()
    {
        C a = new C { Value = 12 };
        FieldInfo info = typeof(C).GetField(""Value"");
        TypedReference reference = __makeref(a);
        if (!(reference is TypedReference reference0)) throw new Exception(""TypedReference"");
        info.SetValueDirect(reference0, 34);
        if (a.Value != 34) throw new Exception(""SetValueDirect"");

        int z = 56;
        if (CopyRefInt(ref z) != 56) throw new Exception(""ref z"");

        Console.WriteLine(""ok"");
    }

    static int CopyRefInt(ref int z)
    {
        if (!(z is int z0)) throw new Exception(""CopyRefInt"");
        return z0;
    }
}";
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "ok");
        }

        [Fact]
        [WorkItem(406203, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=406203")]
        [WorkItem(406205, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=406205")]
        public void DoubleEvaluation()
        {
            var source =
@"using System;
public class X
{
    public static void Main(string[] args)
    {
        {
            int? a = 0;
            if (a++ is int b)
            {
                Console.WriteLine(b);
            }
            Console.WriteLine(a);
        }
        {
            int? a = 0;
            if (++a is int b)
            {
                Console.WriteLine(b);
            }
            Console.WriteLine(a);
        }
        {
            if (Func() is int b)
            {
                Console.WriteLine(b);
            }
        }
    }
    public static int? Func()
    {
        Console.WriteLine(""Func called"");
        return 2;
    }
}
";
            var expectedOutput = @"0
1
1
1
Func called
2";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVoidInIsOrAs_01()
        {
            // though silly, it is not forbidden to test a void value's type
            var source =
@"using System;
class Program
{
    static void Main()
    {
        if (Console.Write(""Hello"") is object) {}
    }
}
";
            var expectedOutput = @"Hello";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,13): warning CS0184: The given expression is never of the provided ('object') type
                //         if (Console.Write("Hello") is object) {}
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, @"Console.Write(""Hello"") is object").WithArguments("object").WithLocation(6, 13)
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVoidInIsOrAs_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var o = Console.WriteLine(""world!"") as object;
        if (o != null) throw null;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,17): error CS0039: Cannot convert type 'void' to 'object' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                //         var o = Console.WriteLine("world!") as object;
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, @"Console.WriteLine(""world!"") as object").WithArguments("void", "object").WithLocation(6, 17)
                );
        }

        [Fact]
        public void TestVoidInIsOrAs_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        M<object>();
    }
    static void M<T>() where T : class
    {
        var o = Console.WriteLine(""Hello"") as T;
        if (o != null) throw null;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (10,17): error CS0039: Cannot convert type 'void' to 'T' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                //         var o = Console.WriteLine("Hello") as T;
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, @"Console.WriteLine(""Hello"") as T").WithArguments("void", "T").WithLocation(10, 17)
                );
        }

        [Fact]
        public void TestVoidInIsOrAs_04()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        if (Console.WriteLine(""Hello"") is var x) { }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found 'void'.
                //         if (Console.WriteLine("Hello") is var x) { }
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, @"Console.WriteLine(""Hello"")").WithArguments("void").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestVoidInIsOrAs_05()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        if (Console.WriteLine(""Hello"") is var _) {}
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found 'void'.
                //         if (Console.WriteLine("Hello") is var _) {}
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, @"Console.WriteLine(""Hello"")").WithArguments("void").WithLocation(6, 13)
                );
        }

        [Fact]
        public void TestVoidInSwitch()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        switch (Console.WriteLine(""Hello""))
        {
            default:
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,17): error CS8119: The switch expression must be a value; found 'void'.
                //         switch (Console.WriteLine("Hello"))
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, @"Console.WriteLine(""Hello"")").WithArguments("void").WithLocation(6, 17)
                );
        }

        [Fact, WorkItem(20103, "https://github.com/dotnet/roslyn/issues/20103")]
        public void TestNullInInPattern()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        const string s = null;
        if (s is string) {} else { Console.Write(""Hello ""); }
        if (s is string t) {} else { Console.WriteLine(""World""); }
    }
}
";
            var expectedOutput = @"Hello World";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('string') type
                //         if (s is string) {} else { Console.Write("Hello "); }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is string").WithArguments("string").WithLocation(7, 13),
                // (8,13): warning CS0184: The given expression is never of the provided ('string') type
                //         if (s is string t) {} else { Console.WriteLine("World"); }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is string t").WithArguments("string").WithLocation(8, 13)
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(22619, "https://github.com/dotnet/roslyn/issues/22619")]
        public void MissingSideEffect()
        {
            var source =
@"using System;
internal class Program
{
    private static void Main()
    {
        try
        {
            var test = new Program();
            var result = test.IsVarMethod();
            Console.WriteLine($""Result = {result}"");
            Console.Read();
        }
        catch (Exception)
        {
            Console.WriteLine(""Exception"");
        }
    }

    private int IsVarMethod() => ThrowingMethod() is var _ ? 1 : 0;
    private bool ThrowingMethod() => throw new Exception(""Oh"");
}
";
            var expectedOutput = @"Exception";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(23100, "https://github.com/dotnet/roslyn/issues/23100")]
        public void TestArrayOfPointer()
        {
            var source =
@"using System;
class Program
{
    unsafe static void Main()
    {
        object o = new byte*[10];
        Console.WriteLine(o is byte*[]); // True
        Console.WriteLine(o is byte*[] _); // True
        Console.WriteLine(o is byte*[] x1); // True
        Console.WriteLine(o is byte**[]); // False
        Console.WriteLine(o is byte**[] _); // False
        Console.WriteLine(o is byte**[] x2); // False
        o = new byte**[10];
        Console.WriteLine(o is byte**[]); // True
        Console.WriteLine(o is byte**[] _); // True
        Console.WriteLine(o is byte**[] x3); // True
        Console.WriteLine(o is byte*[]); // False
        Console.WriteLine(o is byte*[] _); // False
        Console.WriteLine(o is byte*[] x4); // False
    }
}
";
            var expectedOutput = @"True
True
True
False
False
False
True
True
True
False
False
False";
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void DefaultPattern()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        int i = 12;
        if (i is default) {} // error 1
        if (i is (default)) {} // error 2
        if (i is (((default)))) {} // error 3
        switch (i) { case default: break; } // error 4
        switch (i) { case (default): break; } // error 5
        switch (i) { case default when true: break; } // error 6
        switch (i) { case (default) when true: break; } // error 7
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (6,18): error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
                //         if (i is default) {} // error 1
                Diagnostic(ErrorCode.ERR_DefaultInPattern, "default").WithLocation(6, 18),
                // (7,19): error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
                //         if (i is (default)) {} // error 2
                Diagnostic(ErrorCode.ERR_DefaultInPattern, "default").WithLocation(7, 19),
                // (8,21): error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
                //         if (i is (((default)))) {} // error 3
                Diagnostic(ErrorCode.ERR_DefaultInPattern, "default").WithLocation(8, 21),
                // (9,27): error CS8313: A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.
                //         switch (i) { case default: break; } // error 4
                Diagnostic(ErrorCode.ERR_DefaultInSwitch, "default").WithLocation(9, 27),
                // (10,28): error CS8313: A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.
                //         switch (i) { case (default): break; } // error 5
                Diagnostic(ErrorCode.ERR_DefaultInSwitch, "default").WithLocation(10, 28),
                // (11,27): error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
                //         switch (i) { case default when true: break; } // error 6
                Diagnostic(ErrorCode.ERR_DefaultInPattern, "default").WithLocation(11, 27),
                // (12,28): error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
                //         switch (i) { case (default) when true: break; } // error 7
                Diagnostic(ErrorCode.ERR_DefaultInPattern, "default").WithLocation(12, 28)
                );
        }

        [Fact]
        public void EventInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static event System.Func<bool> Test1 = GetDelegate(1 is int x1 && Dummy(x1)); 

    static System.Func<bool> GetDelegate(bool value) => () => value;

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

            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,61): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static event System.Func<bool> Test1 = GetDelegate(1 is int x1 && Dummy(x1)); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "int x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 61)
                );
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
public class C
{
    public ref int M()
    {
        Span<int> outer = stackalloc int[100];
        if (outer is Span<int> inner)
        {
            return ref inner[5];
        }

        throw null;
    }
}").VerifyDiagnostics(
                // (10,24): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(10, 24));
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void CasePatternMatchingDoesNotCopyEscapeScopes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
public class C
{
    public ref int M()
    {
        Span<int> outer = stackalloc int[100];
        switch (outer)
        {
            case Span<int> inner:
            {
                return ref inner[5];
            }
        }

        throw null;
    }
}").VerifyDiagnostics(
                // (12,28): error CS8352: Cannot use local 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeLocal, "inner").WithArguments("inner").WithLocation(12, 28));
        }
    }
}
