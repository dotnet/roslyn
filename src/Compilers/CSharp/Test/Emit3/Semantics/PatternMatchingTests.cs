// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns, CompilerFeature.RefLifetime)]
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
            CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (7,18): error CS8059: Feature 'binary literals' is not available in C# 6. Please use language version 7.0 or greater.
                //         int i1 = 0b001010; // binary literals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("binary literals", "7.0").WithLocation(7, 18),
                // (8,18): error CS8059: Feature 'digit separators' is not available in C# 6. Please use language version 7.0 or greater.
                //         int i2 = 23_554; // digit separators
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "").WithArguments("digit separators", "7.0").WithLocation(8, 18),
                // (13,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(13, 9),
                // (13,22): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int i3 = ref i1; // ref locals
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(13, 22),
                // (12,13): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         int f() => 2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "f").WithArguments("local functions", "7.0").WithLocation(12, 13),
                // (14,22): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         string s = o is string k ? k : null; // pattern matching
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "is").WithArguments("pattern matching", "7.0").WithLocation(14, 22),
                // (12,13): warning CS8321: The local function 'f' is declared but never used
                //         int f() => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(12, 13));

            // enables binary literals, digit separators, local functions, ref locals, pattern matching
            CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (x is Nullable<int> y) Console.WriteLine($"expression {x} is Nullable<int> y");
                Diagnostic(ErrorCode.ERR_PatternNullableType, "Nullable<int>").WithArguments("int").WithLocation(11, 18)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
        if (null is dynamic t2) { } // null not allowed
        if (s is NullableInt x) { } // error: cannot use nullable type
        if (s is long l) { } // error: cannot convert string to long
        if (b is 1000) { } // error: cannot convert 1000 to byte
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is dynamic t2) { } // null not allowed
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(10, 13),
                // (11,18): error CS8116: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (s is NullableInt x) { } // error: cannot use nullable type
                Diagnostic(ErrorCode.ERR_PatternNullableType, "NullableInt").WithArguments("int").WithLocation(11, 18),
                // (12,18): error CS8121: An expression of type 'string' cannot be handled by a pattern of type 'long'.
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28026")]
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28026")]
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
                VerifyModelForDeclarationOrVarSimplePattern(model, yDecl, yRef);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput:
@"1
2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yDecl = GetPatternDeclaration(tree, "y1");
            var yRef = GetReferences(tree, "y1").Single();
            VerifyModelForDeclarationOrVarSimplePattern(model, yDecl, yRef);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,34): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static bool Test1 = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 34)
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,41): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static bool Test1 {get;} = 1 is int x1 && Dummy(x1); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 41)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);

            Assert.Equal("System.Int32", ((ILocalSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(x1Decl[0])).Type.ToTestDisplayString());

            CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (12,40): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     public D(object o) : base(2 is var x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(12, 40),
                // (17,32): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     public D() : this(1 is int x1 && Dummy(x1)) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(17, 32)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"b");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            CompileAndVerify(compilation, expectedOutput:
@"b");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl[0], x3Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl[0], x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x3").ToArray();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl[0], x3Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl, x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl, x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x0Decl[0], x0Ref);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"3
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"1
3");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"yield1
yield2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"return");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"throw");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"throw 1
throw 2");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[1], x1Ref[1]);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput:
@"System.InvalidOperationException
System.InvalidOperationException");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclaration(tree, "x1");
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl, x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,27): warning CS0184: The given expression is never of the provided ('string') type
                //         Console.WriteLine(1L is string); // warning: type mismatch
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1L is string").WithArguments("string").WithLocation(7, 27),
                // (8,27): warning CS0184: The given expression is never of the provided ('int[]') type
                //         Console.WriteLine(1 is int[]); // warning: expression is never of the provided type
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is int[]").WithArguments("int[]").WithLocation(8, 27),
                // (10,33): error CS8121: An expression of type 'long' cannot be handled by a pattern of type 'string'.
                //         Console.WriteLine(1L is string s); // error: type mismatch
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("long", "string").WithLocation(10, 33),
                // (11,32): error CS8121: An expression of type 'int' cannot be handled by a pattern of type 'int[]'.
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,27): warning CS8417: The given expression always matches the provided constant.
                //         Console.WriteLine(1 is 1); // true
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "1 is 1").WithLocation(7, 27),
                // (8,27): warning CS8416: The given expression never matches the provided pattern.
                //         Console.WriteLine(1L is int.MaxValue); // OK, but false
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1L is int.MaxValue").WithLocation(8, 27),
                // (9,27): warning CS8416: The given expression never matches the provided pattern.
                //         Console.WriteLine(1 is int.MaxValue); // false
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is int.MaxValue").WithLocation(9, 27),
                // (10,27): warning CS8417: The given expression always matches the provided constant.
                //         Console.WriteLine(int.MaxValue is int.MaxValue); // true
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "int.MaxValue is int.MaxValue").WithLocation(10, 27),
                // (11,27): warning CS0183: The given expression is always of the provided ('string') type
                //         Console.WriteLine("goo" is System.String); // true
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, @"""goo"" is System.String").WithArguments("string").WithLocation(11, 27),
                // (12,27): warning CS8417: The given expression always matches the provided constant.
                //         Console.WriteLine(Int32.MaxValue is Int32.MaxValue); // true
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "Int32.MaxValue is Int32.MaxValue").WithLocation(12, 27)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (9,18): error CS9133: A constant value of type 'Type' is expected
                //             case typeof(string):
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "typeof(string)").WithArguments("System.Type").WithLocation(9, 18),
                // (12,18): error CS9133: A constant value of type 'Type' is expected
                //             case typeof(string[]):
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "typeof(string[])").WithArguments("System.Type").WithLocation(12, 18)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6);
            compilation.VerifyDiagnostics(
                // (7,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case 1 when true:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case").WithArguments("pattern matching", "7.0").WithLocation(7, 13));
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            Assert.True(((ITypeSymbol)compilation.GetSemanticModel(tree).GetTypeInfo(x1Ref).Type).IsErrorType());
            VerifyModelNotSupported(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelNotSupported(model, x2Decl, x2Ref);
            Assert.True(((ITypeSymbol)compilation.GetSemanticModel(tree).GetTypeInfo(x2Ref).Type).IsErrorType());

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
            CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compilation = CreateCompilation(source).VerifyDiagnostics(
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

            // https://github.com/dotnet/roslyn/issues/27749 : This syntax corresponds to a deconstruction pattern with zero elements, which is not yet supported in IOperation.
            //            compilation.VerifyOperationTree(node, expectedOperationTree:
            //@"
            //IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'o.Equals is()')
            //  Expression: 
            //    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'o.Equals is()')
            //      Children(1):
            //          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'o.Equals')
            //            Children(1):
            //                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o')
            //  Pattern: 
            //");
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
            CreateCompilation(source).VerifyDiagnostics(
                // (7,17): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //             if (null is()) {}
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(7, 17),
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
            CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is 1) {}
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(6, 13),
                // (7,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (Main is 2) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "Main is 2").WithLocation(7, 13),
                // (8,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (delegate {} is 3) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate {} is 3").WithLocation(8, 13),
                // (8,25): warning CS8848: Operator 'is' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         if (delegate {} is 3) {}
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "is").WithArguments("is").WithLocation(8, 25),
                // (9,13): error CS0023: Operator 'is' cannot be applied to operand of type '(int, <null>)'
                //         if ((1, null) is 4) {}
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "(1, null) is 4").WithArguments("is", "(int, <null>)").WithLocation(9, 13),
                // (10,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is var x1) {}
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(10, 13),
                // (11,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (Main is var x2) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "Main is var x2").WithLocation(11, 13),
                // (12,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (delegate {} is var x3) {}
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "delegate {} is var x3").WithLocation(12, 13),
                // (12,25): warning CS8848: Operator 'is' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         if (delegate {} is var x3) {}
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "is").WithArguments("is").WithLocation(12, 25),
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
            CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (15,29): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         Console.WriteLine(3 is One + 2); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "is").WithArguments("pattern matching", "7.0").WithLocation(15, 29),
                // (15,27): warning CS8520: The given expression always matches the provided constant.
                //         Console.WriteLine(3 is One + 2); // should print True
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "3 is One + 2").WithLocation(15, 27),
                // (16,35): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //         Console.WriteLine(One + 2 is 3); // should print True
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "is").WithArguments("pattern matching", "7.0").WithLocation(16, 35),
                // (16,27): warning CS8520: The given expression always matches the provided constant.
                //         Console.WriteLine(One + 2 is 3); // should print True
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "One + 2 is 3").WithLocation(16, 27));
            var expectedOutput =
@"5
6
7
True
True";
            compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (15,27): warning CS8417: The given expression always matches the provided constant.
                //         Console.WriteLine(3 is One + 2); // should print True
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "3 is One + 2").WithLocation(15, 27),
                // (16,27): warning CS8417: The given expression always matches the provided constant.
                //         Console.WriteLine(One + 2 is 3); // should print True
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "One + 2 is 3").WithLocation(16, 27)
                );
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
class @nameof { }
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl, x2Ref);
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
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "is int _: True, is var _: True, case int _, case var _");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var discard1 = GetDiscardDesignations(tree).First();
            Assert.Null(model.GetDeclaredSymbol(discard1));
            var declaration1 = (DeclarationPatternSyntax)discard1.Parent;
            Assert.Equal("int _", declaration1.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration1).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration1.Type).Type.ToTestDisplayString());

            var discard2 = GetDiscardDesignations(tree).Skip(1).First();
            Assert.Null(model.GetDeclaredSymbol(discard2));
            Assert.Null(model.GetSymbolInfo(discard2).Symbol);
            var declaration2 = (VarPatternSyntax)discard2.Parent;
            Assert.Equal("var _", declaration2.ToString());
            Assert.Null(model.GetSymbolInfo(declaration2).Symbol);

            var discard3 = GetDiscardDesignations(tree).Skip(2).First();
            Assert.Null(model.GetDeclaredSymbol(discard3));
            var declaration3 = (DeclarationPatternSyntax)discard3.Parent;
            Assert.Equal("int _", declaration3.ToString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration3).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(declaration3.Type).Type.ToTestDisplayString());

            var discard4 = GetDiscardDesignations(tree).Skip(3).First();
            Assert.Null(model.GetDeclaredSymbol(discard4));
            var declaration4 = (VarPatternSyntax)discard4.Parent;
            Assert.Equal("var _", declaration4.ToString());
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
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (8,29): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         Write($"is _: {i is _}, ");
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(8, 29),
                // (11,18): error CS0103: The name '_' does not exist in the current context
                //             case _:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(11, 18)
                );
            CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (8,29): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         Write($"is _: {i is _}, ");
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(8, 29),
                // (11,18): error CS0103: The name '_' does not exist in the current context
                //             case _:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(11, 18)
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
                // (9,29): error CS0118: '_' is a variable but is used like a type
                //         Write($"is _: {i is _}, ");
                Diagnostic(ErrorCode.ERR_BadSKknown, "_").WithArguments("_", "variable", "type").WithLocation(9, 29),
                // (12,18): error CS9133: A constant value of type 'int' is expected
                //             case _:
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "_").WithArguments("int").WithLocation(12, 18)
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
public class @var {}
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
                // (5,42): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('var')
                //     public static void Main(int* a, var* c, Typ* e)
                Diagnostic(ErrorCode.WRN_ManagedAddr, "c").WithArguments("var").WithLocation(5, 42),
                // (5,50): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('Typ')
                //     public static void Main(int* a, var* c, Typ* e)
                Diagnostic(ErrorCode.WRN_ManagedAddr, "e").WithArguments("Typ").WithLocation(5, 50),
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
                Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(15, 36)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(6, 30),
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
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Missing").WithArguments("Missing").WithLocation(11, 18)
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
            CreateCompilation(program).VerifyDiagnostics(
                // (6,17): error CS8119: The switch expression must be a value; found 'lambda expression'.
                //         switch ((() => 1))
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "(() => 1)").WithArguments("lambda expression").WithLocation(6, 17),
                // (10,18): error CS0150: A constant value is expected
                //             case M:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M").WithLocation(10, 18),
                // (11,19): error CS0150: A constant value is expected
                //             case ((int)M()):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int)M()").WithLocation(11, 19)
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
            CreateCompilation(program).VerifyDiagnostics(
                // (6,17): error CS8119: The switch expression must be a value; found 'lambda expression'.
                //         switch ((() => 1))
                Diagnostic(ErrorCode.ERR_SwitchExpressionValueExpected, "(() => 1)").WithArguments("lambda expression").WithLocation(6, 17),
                // (8,18): error CS0150: A constant value is expected
                //             case M:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M").WithLocation(8, 18)
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
            CreateCompilation(program).VerifyDiagnostics(
                // (6,13): error CS8117: Invalid operand for pattern match; value required, but found '<null>'.
                //         if (null is M) {}
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "null").WithArguments("<null>").WithLocation(6, 13),
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
            CreateCompilation(program).VerifyDiagnostics(
                // (10,18): error CS0266: Cannot implicitly convert type 'double' to 'int?'. An explicit conversion exists (are you missing a cast?)
                //             case double.NaN:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "double.NaN").WithArguments("double", "int?").WithLocation(10, 18),
                // (13,18): error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'string'.
                //             case string _:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("int?", "string").WithLocation(13, 18)
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
            var compilation = CreateCompilation(program).VerifyDiagnostics(
                // (8,18): error CS0118: 'Color' is a variable but is used like a type
                //             case Color Color:
                Diagnostic(ErrorCode.ERR_BadSKknown, "Color").WithArguments("Color", "variable", "type").WithLocation(8, 18),
                // (9,25): error CS0103: The name 'Color2' does not exist in the current context
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Color2").WithArguments("Color2").WithLocation(9, 25),
                // (9,32): error CS1525: Invalid expression term 'break'
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("break").WithLocation(9, 32),
                // (9,32): error CS1003: Syntax error, ':' expected
                //             case Color? Color2:
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(9, 32));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var colorDecl = GetPatternDeclarations(tree, "Color").ToArray();
            var colorRef = GetReferences(tree, "Color").ToArray();

            Assert.Equal(1, colorDecl.Length);
            Assert.Equal(2, colorRef.Length);

            Assert.Null(model.GetSymbolInfo(colorRef[0]).Symbol);
            VerifyModelForDeclarationOrVarSimplePattern(model, colorDecl[0], colorRef[1]);
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
            var compilation = CreateCompilation(program).VerifyDiagnostics(
                // (9,18): error CS9133: A constant value of type 'int' is expected
                //             case true ? x3 : 4:
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "true ? x3 : 4").WithArguments("int").WithLocation(9, 18),
                // (9,25): error CS0165: Use of unassigned local variable 'x3'
                //             case true ? x3 : 4:
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(9, 25)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").ToArray();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(1, x3Decl.Length);
            Assert.Equal(1, x3Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x3Decl[0], x3Ref);
        }

        [Fact]
        public void Fuzz_Conjunction_01()
        {
            var program = @"
public class Program
{
    public static void Main(string[] args)
    {
        if (((int?)1) is {} and 1) { }
    }
}";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                );
        }

        [Fact]
        public void Fuzz_738490379()
        {
            var program = @"
public class Program738490379
{
    public static void Main(string[] args)
    {
        if (NotFound is var (M, not int _ or NotFound _) {  }) {}
    }
    private static object M() => null;
}";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                    // (6,13): error CS0841: Cannot use local variable 'NotFound' before it is declared
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "NotFound").WithArguments("NotFound").WithLocation(6, 13),
                    // (6,37): error CS1026: ) expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(6, 37),
                    // (6,37): error CS1026: ) expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(6, 37),
                    // (6,37): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "int _ ").WithLocation(6, 37),
                    // (6,41): warning CS0168: The variable '_' is declared but never used
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "_").WithArguments("_").WithLocation(6, 41),
                    // (6,43): error CS1002: ; expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "or").WithLocation(6, 43),
                    // (6,43): error CS0246: The type or namespace name 'or' could not be found (are you missing a using directive or an assembly reference?)
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "or").WithArguments("or").WithLocation(6, 43),
                    // (6,55): error CS1002: ; expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "_").WithLocation(6, 55),
                    // (6,55): error CS0103: The name '_' does not exist in the current context
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(6, 55),
                    // (6,56): error CS1002: ; expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 56),
                    // (6,56): error CS1513: } expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 56),
                    // (6,62): error CS1513: } expected
                    //         if (NotFound is var (M, not int _ or NotFound _) {  }) {}
                    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 62)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/16721")]
        public void Fuzz()
        {
            const int numTests = 1200000;
            int dt = (int)Math.Abs(DateTime.Now.Ticks % 1000000000);
            for (int i = 1; i < numTests; i++)
            {
                PatternMatchingFuzz(i + dt);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/16721")]
        public void MultiFuzz()
        {
            // Just like Fuzz(), but take advantage of concurrency on the test host.
            const int numTasks = 300;
            const int numTestsPerTask = 4000;
            int dt = (int)Math.Abs(DateTime.Now.Ticks % 1000000000);
            var tasks = Enumerable.Range(0, numTasks).Select(t => Task.Run(() =>
            {
                int k = dt + t * numTestsPerTask;
                for (int i = 1; i < numTestsPerTask; i++)
                {
                    PatternMatchingFuzz(i + k);
                }
            }));
            Task.WaitAll(tasks.ToArray());
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
            string Pattern(int d = 5)
            {
                switch (r.Next(d <= 1 ? 9 : 13))
                {
                    default:
                        return Expression(); // a "constant" pattern
                    case 3:
                    case 4:
                        return Type();
                    case 5:
                        return Type() + " _";
                    case 6:
                        return Type() + " x" + r.Next(10);
                    case 7:
                        return "not " + Pattern(d - 1);
                    case 8:
                        return "(" + Pattern(d - 1) + ")";
                    case 9:
                        return r.Next(2) == 0 ? makeRecursivePattern(d) : makeListPattern(d);
                    case 10:
                        return Pattern(d - 1) + " and " + Pattern(d - 1);
                    case 11:
                        return Pattern(d - 1) + " or " + Pattern(d - 1);
                    case 12:
                        return ".." + (r.Next(2) == 0 ? Pattern(d - 1) : null);
                }

                string makeRecursivePattern(int d)
                {
                    while (true)
                    {
                        bool haveParens = r.Next(2) == 0;
                        bool haveCurlies = r.Next(2) == 0;
                        if (!haveParens && !haveCurlies)
                            continue;
                        bool haveType = r.Next(2) == 0;
                        bool haveIdentifier = r.Next(2) == 0;
                        return $"{(haveType ? Type() : null)} {(haveParens ? $"({makePatternList(d - 1, false)})" : null)} {(haveCurlies ? $"{"{ "}{makePatternList(d - 1, true)}{" }"}" : null)} {(haveIdentifier ? " x" + r.Next(10) : null)}";
                    }
                }

                string makeListPattern(int d)
                {
                    bool haveIdentifier = r.Next(2) == 0;
                    return $"[{makePatternList(d - 1, false)}]{(haveIdentifier ? " x" + r.Next(10) : null)}";
                }

                string makePatternList(int d, bool propNames)
                {
                    return string.Join(", ", Enumerable.Range(0, r.Next(3)).Select(i => $"{(propNames ? $"P{r.Next(10)}: " : null)}{Pattern(d)}"));
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
            CreateCompilation(program).GetDiagnostics();
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
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe).VerifyDiagnostics(
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
            var compilation = CreateCompilation(program).VerifyDiagnostics(
                // (12,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case TDerived td:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "TDerived td").WithLocation(12, 18)
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
            var compilation = CreateCompilation(program).VerifyDiagnostics(
                // (11,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case IEnumerable<object> s:
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "IEnumerable<object> s").WithLocation(11, 18)
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
            var compilation = CreateCompilation(program, options: TestOptions.DebugExe);
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
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("string", "T", "7.0", "7.1").WithLocation(10, 18)
                );
            CreateCompilation(program, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular7_1).VerifyDiagnostics(
                // (10,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case T tt: // Produces a diagnostic about subsumption/unreachability
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "T tt").WithLocation(10, 18)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (7,32): error CS0266: Cannot implicitly convert type 'long' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         Console.WriteLine(b is 12L);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "12L").WithArguments("long", "byte").WithLocation(7, 32),
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
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
            case 0f/0f: break;
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
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
namespace System
{
    public struct Single
    {
        private Single m_value;
        public /*note bad return type*/ void Equals(Single other) { m_value = m_value + 1; }
        public /*note bad return type*/ void IsNaN(Single other) { }
    }
}
";
            var compilation = CreateEmptyCompilation(source);
            compilation.VerifyDiagnostics(
                );
            compilation.GetEmitDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Warning).Verify(
                // (7,18): error CS0656: Missing compiler required member 'System.Single.IsNaN'
                //             case 0f/0f: break;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "0f/0f").WithArguments("System.Single", "IsNaN").WithLocation(7, 18)
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
            var compilation = CreateCompilation(source);
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
            case 2:     // subsumed
                break;
        }
    }
    static void M2(object o)
    {
        switch (o)
        {
            case 1:
            case int _:
            case int _:  // subsumed
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case 2:     // subsumed
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "2").WithLocation(9, 18),
                // (19,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case int _:  // subsumed
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "int _").WithLocation(19, 18)
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
            var compilation = CreateCompilation(source, references: new MetadataReference[] { CSharpRef }, options: TestOptions.ReleaseExe);
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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Derived' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Derived b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived").WithArguments("T", "Derived", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Derived' in C# 7.0. Please use language version 7.1 or greater.
                //             case Derived b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived").WithArguments("T", "Derived", "7.0", "7.1").WithLocation(16, 18)
                );
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS8413: An expression of type 'Base' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is T b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("Base", "T", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS8413: An expression of type 'Base' cannot be handled by a pattern of type 'T' in C# 7.0. Please use language version 7.1 or greater.
                //             case T b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "T").WithArguments("Base", "T", "7.0", "7.1")
                );
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (13,28): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Derived<T>' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Derived<T> b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived<T>").WithArguments("T", "Derived<T>", "7.0", "7.1").WithLocation(13, 28),
                // (16,18): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Derived<T>' in C# 7.0. Please use language version 7.1 or greater.
                //             case Derived<T> b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Derived<T>").WithArguments("T", "Derived<T>", "7.0", "7.1").WithLocation(16, 18)
                );
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (16,28): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Container<T>.Derived' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Container<T>.Derived b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived").WithArguments("T", "Container<T>.Derived", "7.0", "7.1").WithLocation(16, 28),
                // (19,18): error CS8413: An expression of type 'T' cannot be handled by a pattern of type 'Container<T>.Derived' in C# 7.0. Please use language version 7.1 or greater.
                //             case Container<T>.Derived b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived").WithArguments("T", "Container<T>.Derived", "7.0", "7.1").WithLocation(19, 18)
                );
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7);
            compilation.VerifyDiagnostics(
                // (16,28): error CS8413: An expression of type 'T[]' cannot be handled by a pattern of type 'Container<T>.Derived[]' in C# 7.0. Please use language version 7.1 or greater.
                //         Console.Write(x is Container<T>.Derived[] b0);
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived[]").WithArguments("T[]", "Container<T>.Derived[]", "7.0", "7.1").WithLocation(16, 28),
                // (19,18): error CS8413: An expression of type 'T[]' cannot be handled by a pattern of type 'Container<T>.Derived[]' in C# 7.0. Please use language version 7.1 or greater.
                //             case Container<T>.Derived[] b1:
                Diagnostic(ErrorCode.ERR_PatternWrongGenericTypeInVersion, "Container<T>.Derived[]").WithArguments("T[]", "Container<T>.Derived[]", "7.0", "7.1").WithLocation(19, 18)
                );
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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
                // (9,13): warning CS8416: The given expression never matches the provided pattern.
                //         if (s is string s2) { }
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "s is string s2").WithLocation(9, 13),
                // (14,13): warning CS0184: The given expression is never of the provided ('long') type
                //         if (i is long) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "i is long").WithArguments("long").WithLocation(14, 13),
                // (15,18): error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'long'.
                //         if (i is long l) { }
                Diagnostic(ErrorCode.ERR_PatternWrongType, "long").WithArguments("int?", "long").WithLocation(15, 18),
                // (16,17): error CS0103: The name 'b' does not exist in the current context
                //         switch (b) { case long m: break; }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(16, 17)
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
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref int").WithArguments("ref").WithLocation(7, 23),
                // (7,27): error CS1525: Invalid expression term 'int'
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 27),
                // (7,31): error CS1002: ; expected
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "z0").WithLocation(7, 31),
                // (6,28): error CS0103: The name 'p0' does not exist in the current context
                //         var p1 = p is int* p0;           // syntax error 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p0").WithArguments("p0").WithLocation(6, 28),
                // (7,23): error CS1073: Unexpected token 'ref'
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(7, 23),
                // (7,31): error CS0103: The name 'z0' does not exist in the current context
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z0").WithArguments("z0").WithLocation(7, 31),
                // (7,31): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         var r1 = z is ref int z0;        // syntax error 2
                Diagnostic(ErrorCode.ERR_IllegalStatement, "z0").WithLocation(7, 31),
                // (9,23): error CS8121: An expression of type 'TypedReference' cannot be handled by a pattern of type 'object'.
                //         var b1 = x is object o1;         // not allowed 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("System.TypedReference", "object").WithLocation(9, 23),
                // (10,23): error CS8521: Pattern-matching is not permitted for pointer types.
                //         var b2 = p is object o2;         // not allowed 2
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "object").WithLocation(10, 23)
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
            var comp = CompileAndVerify(compilation, expectedOutput: "ok", verify: Verification.FailsILVerify);
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
                Diagnostic(ErrorCode.ERR_BadPatternExpression, @"Console.WriteLine(""Hello"")").WithArguments("void").WithLocation(6, 13)
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
                Diagnostic(ErrorCode.ERR_BadPatternExpression, @"Console.WriteLine(""Hello"")").WithArguments("void").WithLocation(6, 13)
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
        public void TestNullInIsPattern()
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
                // (8,13): warning CS8416: The given expression never matches the provided pattern.
                //         if (s is string t) {} else { Console.WriteLine("World"); }
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "s is string t").WithLocation(8, 13)
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
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
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
            // PEVerify:
            // [ : Program::Main][mdToken=0x6000001][offset 0x00000002] Unmanaged pointers are not a verifiable type.
            // [ : Program::Main][mdToken= 0x6000001][offset 0x00000002] Unable to resolve token.
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify);
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
                // (6,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is default) {} // error 1
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(6, 18),
                // (7,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default)) {} // error 2
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(7, 19),
                // (8,21): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (((default)))) {} // error 3
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(8, 21),
                // (9,27): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case default: break; } // error 4
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(9, 27),
                // (10,28): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case (default): break; } // error 5
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 28),
                // (11,27): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case default when true: break; } // error 6
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(11, 27),
                // (12,28): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case (default) when true: break; } // error 7
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 28)
                );

            var tree = compilation.SyntaxTrees.Single();
            var caseDefault = tree.GetRoot().DescendantNodes().OfType<CasePatternSwitchLabelSyntax>().First();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
            Assert.Equal("System.Int32", model.GetTypeInfo(caseDefault.Pattern).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(caseDefault.Pattern).ConvertedType.ToTestDisplayString());
            Assert.False(model.GetConstantValue(caseDefault.Pattern).HasValue);
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");

            CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_2).VerifyDiagnostics(
                // (9,65): error CS8320: Feature 'declaration of expression variables in member initializers and queries' is not available in C# 7.2. Please use language version 7.3 or greater.
                //     static event System.Func<bool> Test1 = GetDelegate(1 is int x1 && Dummy(x1)); 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "x1").WithArguments("declaration of expression variables in member initializers and queries", "7.3").WithLocation(9, 65)
                );
        }

        [Fact]
        public void ExhaustiveBoolSwitch00()
        {
            // Note that the switches in this code are exhaustive. The idea of a switch
            // being exhaustive is new with the addition of pattern-matching; this code
            // used to give errors that are no longer applicable due to the spec change.
            var source =
@"
using System;

public class C
{
    public static void Main()
    {
        M(true);
        M(false);
        Console.WriteLine(M2(true));
        Console.WriteLine(M2(false));
    }
    public static void M(bool e)
    {
        bool b;
        switch (e)
        {
            case true:
                b = true;
                break;
            case false:
                b = false;
                break;
        }

        Console.WriteLine(b); // no more error CS0165: Use of unassigned local variable 'b'
    }

    public static bool M2(bool e) // no more error CS0161: not all code paths return a value
    {
        switch (e)
        {
            case true: return true;
            case false: return false;
        }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"True
False
True
False");
        }

        [Fact, WorkItem(24865, "https://github.com/dotnet/roslyn/issues/24865")]
        public void ExhaustiveBoolSwitch01()
        {
            var source =
@"
using System;

public class C
{
    public static void Main()
    {
        M(true);
        M(false);
        Console.WriteLine(M2(true));
        Console.WriteLine(M2(false));
    }
    public static void M(bool e)
    {
        bool b;
        switch (e)
        {
            case true when true:
                b = true;
                break;
            case false:
                b = false;
                break;
        }

        Console.WriteLine(b);
    }

    public static bool M2(bool e)
    {
        switch (e)
        {
            case true when true: return true;
            case false: return false;
        }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"True
False
True
False");
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes_01()
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
                // (10,24): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(10, 24));
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes_03()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithPatternCombinators,
                text: @"
using System;
public class C
{
    public ref int M()
    {
        Span<int> outer = stackalloc int[100];
        if (outer is ({} and var x) and Span<int> inner)
        {
            return ref inner[5];
        }

        throw null;
    }
}").VerifyDiagnostics(
                // (8,13): warning CS8794: An expression of type 'Span<int>' always matches the provided pattern.
                //         if (outer is ({} and var x) and Span<int> inner)
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is ({} and var x) and Span<int> inner").WithArguments("System.Span<int>").WithLocation(8, 13),
                // (10,24): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //             return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(10, 24)
                );
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void CasePatternMatchingDoesNotCopyEscapeScopes_01()
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
                // (12,28): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(12, 28));
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void CasePatternMatchingDoesNotCopyEscapeScopes_03()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithPatternCombinators, text: @"
using System;
public class C
{
    public ref int M()
    {
        Span<int> outer = stackalloc int[100];
        switch (outer)
        {
            case {} and Span<int> inner:
            {
                return ref inner[5];
            }
        }

        throw null;
    }
}").VerifyDiagnostics(
                // (10,18): hidden CS9335: The pattern is redundant.
                //             case {} and Span<int> inner:
                Diagnostic(ErrorCode.HDN_RedundantPattern, "{}").WithLocation(10, 18),
                // (12,28): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //                 return ref inner[5];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "inner").WithArguments("inner").WithLocation(12, 28));
        }

        [Fact]
        [WorkItem(28633, "https://github.com/dotnet/roslyn/issues/28633")]
        public void CasePatternMatchingDoesNotCopyEscapeScopes_02()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithRecursivePatterns, text: @"
using System;
public ref struct R
{
    public R Prop => this;
    public void Deconstruct(out R X, out R Y) => X = Y = this;
    public static implicit operator R(Span<int> span) => new R();
}
public class C
{
    public R M1()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case { Prop: var x }: return x; // error 1
        }
    }
    public R M2()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case { Prop: R x }: return x; // error 2
        }
    }
    public R M3()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case (var x, var y): return x; // error 3
        }
    }
    public R M4()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case (R x, R y): return x; // error 4
        }
    }
    public R M5()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var (x, y): return x; // error 5
        }
    }
    public R M6()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case { } x: return x; // error 6
        }
    }
    public R M7()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case (_, _) x: return x; // error 7
        }
    }
}
").VerifyDiagnostics(
                // (16,42): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case { Prop: var x }: return x; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(16, 42),
                // (24,40): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case { Prop: R x }: return x; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(24, 40),
                // (32,41): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case (var x, var y): return x; // error 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(32, 41),
                // (40,37): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case (R x, R y): return x; // error 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(40, 37),
                // (48,37): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var (x, y): return x; // error 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(48, 37),
                // (56,32): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case { } x: return x; // error 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(56, 32),
                // (64,35): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case (_, _) x: return x; // error 7
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(64, 35)
                );
        }

        [Fact]
        [WorkItem(28633, "https://github.com/dotnet/roslyn/issues/28633")]
        public void CasePatternMatchingDoesNotCopyEscapeScopes_04()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithPatternCombinators, text: @"
#pragma warning disable CS9335 // hidden CS9335: The pattern is redundant.
using System;
public ref struct R
{
    public R Prop => this;
    public void Deconstruct(out R X, out R Y) => X = Y = this;
    public static implicit operator R(Span<int> span) => new R();
}
public class C
{
    public R M1()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and { Prop: var _ and {} and var x }: return x; // error 1
        }
    }
    public R M2()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and { Prop: var _ and {} and R x }: return x; // error 2
        }
    }
    public R M3()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and (var _ and {} and var x, var _ and {} and var y): return x; // error 3
        }
    }
    public R M4()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and (var _ and {} and R x, var _ and {} and R y): return x; // error 4
        }
    }
    public R M5()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and var (x, y): return x; // error 5
        }
    }
    public R M6()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and { } x: return x; // error 6
        }
    }
    public R M7()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case var _ and {} and (var _ and {} and _, var _ and {} and _) x: return x; // error 7
        }
    }
}
").VerifyDiagnostics(
                // (17,76): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and { Prop: var _ and {} and var x }: return x; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(17, 76),
                // (25,74): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and { Prop: var _ and {} and R x }: return x; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(25, 74),
                // (33,92): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and (var _ and {} and var x, var _ and {} and var y): return x; // error 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(33, 92),
                // (41,88): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and (var _ and {} and R x, var _ and {} and R y): return x; // error 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(41, 88),
                // (49,54): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and var (x, y): return x; // error 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(49, 54),
                // (57,49): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and { } x: return x; // error 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(57, 49),
                // (65,86): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //             case var _ and {} and (var _ and {} and _, var _ and {} and _) x: return x; // error 7
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(65, 86)
                );
        }

        [Fact]
        [WorkItem(28633, "https://github.com/dotnet/roslyn/issues/28633")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes_02()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithRecursivePatterns, text: @"
using System;
public ref struct R
{
    public R Prop => this;
    public void Deconstruct(out R X, out R Y) => X = Y = this;
    public static implicit operator R(Span<int> span) => new R();
}
public class C
{
    public R M1()
    {
        R outer = stackalloc int[100];
        if (outer is { Prop: var x }) return x; // error 1
        throw null;
    }
    public R M2()
    {
        R outer = stackalloc int[100];
        if (outer is { Prop: R x }) return x; // error 2
        throw null;
    }
    public R M3()
    {
        R outer = stackalloc int[100];
        if (outer is (var x, var y)) return x; // error 3
        throw null;
    }
    public R M4()
    {
        R outer = stackalloc int[100];
        if (outer is (R x, R y)) return x; // error 4
        throw null;
    }
    public R M5()
    {
        R outer = stackalloc int[100];
        if (outer is var (x, y)) return x; // error 5
        throw null;
    }
    public R M6()
    {
        R outer = stackalloc int[100];
        if (outer is { } x) return x; // error 6
        throw null;
    }
    public R M7()
    {
        R outer = stackalloc int[100];
        if (outer is (_, _) x) return x; // error 7
        throw null;
    }
}
").VerifyDiagnostics(
                // (14,46): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is { Prop: var x }) return x; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(14, 46),
                // (20,44): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is { Prop: R x }) return x; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(20, 44),
                // (26,45): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is (var x, var y)) return x; // error 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(26, 45),
                // (32,41): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is (R x, R y)) return x; // error 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(32, 41),
                // (38,41): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var (x, y)) return x; // error 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(38, 41),
                // (44,36): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is { } x) return x; // error 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(44, 36),
                // (50,39): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is (_, _) x) return x; // error 7
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(50, 39)
                );
        }

        [Fact]
        [WorkItem(28633, "https://github.com/dotnet/roslyn/issues/28633")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes_04()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithPatternCombinators, text: @"
using System;
public ref struct R
{
    public R Prop => this;
    public void Deconstruct(out R X, out R Y) => X = Y = this;
    public static implicit operator R(Span<int> span) => new R();
}
public class C
{
    public R M1()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and { Prop: var _ and {} and var x }) return x; // error 1
        throw null;
    }
    public R M2()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and { Prop: var _ and {} and R x }) return x; // error 2
        throw null;
    }
    public R M3()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and (var _ and {} and var x, var _ and {} and var y)) return x; // error 3
        throw null;
    }
    public R M4()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and (var _ and {} and R x, var _ and {} and R y)) return x; // error 4
        throw null;
    }
    public R M5()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and var (x, y)) return x; // error 5
        throw null;
    }
    public R M6()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and { } x) return x; // error 6
        throw null;
    }
    public R M7()
    {
        R outer = stackalloc int[100];
        if (outer is var _ and {} and (_, _) x) return x; // error 7
        throw null;
    }
}
").VerifyDiagnostics(
                //         if (outer is var _ and {} and { Prop: var _ and {} and var x }) return x; // error 1
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and { Prop: var _ and {} and var x }").WithArguments("R").WithLocation(14, 13),
                // (14,80): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and { Prop: var _ and {} and var x }) return x; // error 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(14, 80),
                // (20,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and { Prop: var _ and {} and R x }) return x; // error 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and { Prop: var _ and {} and R x }").WithArguments("R").WithLocation(20, 13),
                // (20,78): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and { Prop: var _ and {} and R x }) return x; // error 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(20, 78),
                // (26,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and (var _ and {} and var x, var _ and {} and var y)) return x; // error 3
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and (var _ and {} and var x, var _ and {} and var y)").WithArguments("R").WithLocation(26, 13),
                // (26,96): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and (var _ and {} and var x, var _ and {} and var y)) return x; // error 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(26, 96),
                // (32,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and (var _ and {} and R x, var _ and {} and R y)) return x; // error 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and (var _ and {} and R x, var _ and {} and R y)").WithArguments("R").WithLocation(32, 13),
                // (32,92): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and (var _ and {} and R x, var _ and {} and R y)) return x; // error 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(32, 92),
                // (38,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and var (x, y)) return x; // error 5
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and var (x, y)").WithArguments("R").WithLocation(38, 13),
                // (38,58): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and var (x, y)) return x; // error 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(38, 58),
                // (44,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and { } x) return x; // error 6
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and { } x").WithArguments("R").WithLocation(44, 13),
                // (44,53): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and { } x) return x; // error 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(44, 53),
                // (50,13): warning CS8794: An expression of type 'R' always matches the provided pattern.
                //         if (outer is var _ and {} and (_, _) x) return x; // error 7
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "outer is var _ and {} and (_, _) x").WithArguments("R").WithLocation(50, 13),
                // (50,56): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is var _ and {} and (_, _) x) return x; // error 7
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(50, 56)
                );
        }

        [Fact]
        [WorkItem(27218, "https://github.com/dotnet/roslyn/issues/27218")]
        public void IsPatternMatchingDoesNotCopyEscapeScopes_05()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
public ref struct R
{
    public R RProp => throw null;
    public S SProp => throw null;
    public static implicit operator R(Span<int> span) => throw null;
}
public struct S
{
    public R RProp => throw null;
    public S SProp => throw null;
}
public class C
{
    public void M1(ref R r, ref S s)
    {
        R outer = stackalloc int[100];
        if (outer is { RProp.RProp: var rr0 }) r = rr0; // error
        if (outer is { SProp.RProp: var sr0 }) r = sr0; // OK
        if (outer is { SProp.SProp: var ss0 }) s = ss0; // OK
        if (outer is { RProp.SProp: var rs0 }) s = rs0; // OK
        if (outer is { RProp: { RProp: var rr1 }}) r = rr1; // error
        if (outer is { SProp: { RProp: var sr1 }}) r = sr1; // OK
        if (outer is { SProp: { SProp: var ss1 }}) s = ss1; // OK
        if (outer is { RProp: { SProp: var rs1 }}) s = rs1; // OK
    }
}").VerifyDiagnostics(
                // (19,52): error CS8352: Cannot use variable 'rr0' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is { RProp.RProp: var rr0 }) r = rr0; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rr0").WithArguments("rr0").WithLocation(19, 52),
                // (23,56): error CS8352: Cannot use variable 'rr1' in this context because it may expose referenced variables outside of their declaration scope
                //         if (outer is { RProp: { RProp: var rr1 }}) r = rr1; // error
                Diagnostic(ErrorCode.ERR_EscapeVariable, "rr1").WithArguments("rr1").WithLocation(23, 56));
        }

        [Fact]
        [WorkItem(28633, "https://github.com/dotnet/roslyn/issues/28633")]
        public void EscapeScopeInSubpatternOfNonRefType()
        {
            CreateCompilationWithMscorlibAndSpan(parseOptions: TestOptions.RegularWithRecursivePatterns, text: @"
using System;
public ref struct R
{
    public R RProp => this;
    public S SProp => new S();
    public void Deconstruct(out S X, out S Y) => X = Y = new S();
    public static implicit operator R(Span<int> span) => new R();
}
public struct S
{
    public R RProp => new R();
}
public class C
{
    public R M1()
    {
        R outer = stackalloc int[100];
        if (outer is { SProp: { RProp: var x }}) return x; // OK
        throw null;
    }
    public R M2()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case { SProp: { RProp: var x }}: return x; // OK
        }
    }
    public R M3()
    {
        R outer = stackalloc int[100];
        if (outer is ({ RProp: var x }, _)) return x; // OK
        throw null;
    }
    public R M4()
    {
        R outer = stackalloc int[100];
        switch (outer)
        {
            case ({ RProp: var x }, _): return x; // OK
        }
    }
}
").VerifyDiagnostics(
                );
        }

        [Fact]
        [WorkItem(39960, "https://github.com/dotnet/roslyn/issues/39960")]
        public void MissingExceptionType()
        {
            var source = @"
class C
{
    void M(bool b, dynamic d)
    {
        _ = b
            ? throw new System.NullReferenceException()
            : throw null;
        L();
        throw null;
        void L() => throw d;
    }
}
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Exception);
            comp.VerifyDiagnostics(
                // (7,21): error CS0518: Predefined type 'System.Exception' is not defined or imported
                //             ? throw new System.NullReferenceException()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "new System.NullReferenceException()").WithArguments("System.Exception").WithLocation(7, 21),
                // (8,21): error CS0518: Predefined type 'System.Exception' is not defined or imported
                //             : throw null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "null").WithArguments("System.Exception").WithLocation(8, 21),
                // (10,15): error CS0518: Predefined type 'System.Exception' is not defined or imported
                //         throw null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "null").WithArguments("System.Exception").WithLocation(10, 15),
                // (11,27): error CS0518: Predefined type 'System.Exception' is not defined or imported
                //         void L() => throw d;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "d").WithArguments("System.Exception").WithLocation(11, 27)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.MakeTypeMissing(WellKnownType.System_Exception);
            comp.VerifyDiagnostics(
                // (7,21): error CS0155: The type caught or thrown must be derived from System.Exception
                //             ? throw new System.NullReferenceException()
                Diagnostic(ErrorCode.ERR_BadExceptionType, "new System.NullReferenceException()").WithLocation(7, 21),
                // (11,27): error CS0155: The type caught or thrown must be derived from System.Exception
                //         void L() => throw d;
                Diagnostic(ErrorCode.ERR_BadExceptionType, "d").WithLocation(11, 27)
                );
        }

        [Fact]
        public void MissingExceptionType_In7()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
            Test();
        }
        catch
        {
            System.Console.WriteLine(""in catch"");
        }
    }

    static void Test()
    {
        throw null;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7);
            comp.MakeTypeMissing(WellKnownType.System_Exception);
            comp.VerifyDiagnostics(
                );
            CompileAndVerify(comp, expectedOutput: "in catch");
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnConstantString()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""test string"");
        Test(""test string? I think not!"");
        Test(""WrongString"");
    }
    static void Test(ReadOnlySpan<char> chars) => Console.WriteLine(chars is ""test string"");
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"False
True
False
False")
                .VerifyIL("C.Test", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""test string""
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0010:  call       ""void System.Console.WriteLine(bool)""
  IL_0015:  nop
  IL_0016:  ret
}");
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnConstantString()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""String 1"");
        Test(""string 1"");
        Test(""string 2"");
        Test(""STRING 2"");
        Test(""string 3"");
    }
    static void Test(ReadOnlySpan<char> chars) 
    {
        var number = chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""STRING 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
3
1
3
2
3")
                .VerifyIL("C.Test", @"
 {
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0007:  brfalse.s  IL_002f
  IL_0009:  ldarg.0
  IL_000a:  ldstr      ""string 1""
  IL_000f:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0014:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0019:  brtrue.s   IL_0033
  IL_001b:  ldarg.0
  IL_001c:  ldstr      ""STRING 2""
  IL_0021:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0026:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_002b:  brtrue.s   IL_0037
  IL_002d:  br.s       IL_003b
  IL_002f:  ldc.i4.0
  IL_0030:  stloc.0
  IL_0031:  br.s       IL_003d
  IL_0033:  ldc.i4.1
  IL_0034:  stloc.0
  IL_0035:  br.s       IL_003d
  IL_0037:  ldc.i4.2
  IL_0038:  stloc.0
  IL_0039:  br.s       IL_003d
  IL_003b:  ldc.i4.3
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  call       ""void System.Console.WriteLine(int)""
  IL_0043:  ret
}");
        }

        // Similar to above but switching on a local value rather than a parameter.
        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchReadOnlySpanChar_Local()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        ReadOnlySpan<char> chars = ""string 2"";
        var number = chars switch
        {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"2")
                .VerifyIL("C.Main",
@"{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (System.ReadOnlySpan<char> V_0, //chars
                int V_1)
  IL_0000:  ldstr      ""string 2""
  IL_0005:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0012:  brfalse.s  IL_003a
  IL_0014:  ldloc.0
  IL_0015:  ldstr      ""string 1""
  IL_001a:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001f:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0024:  brtrue.s   IL_003e
  IL_0026:  ldloc.0
  IL_0027:  ldstr      ""string 2""
  IL_002c:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0031:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0036:  brtrue.s   IL_0042
  IL_0038:  br.s       IL_0046
  IL_003a:  ldc.i4.0
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_0048
  IL_003e:  ldc.i4.1
  IL_003f:  stloc.1
  IL_0040:  br.s       IL_0048
  IL_0042:  ldc.i4.2
  IL_0043:  stloc.1
  IL_0044:  br.s       IL_0048
  IL_0046:  ldc.i4.3
  IL_0047:  stloc.1
  IL_0048:  ldloc.1
  IL_0049:  call       ""void System.Console.WriteLine(int)""
  IL_004e:  ret
}");
        }

        // Similar to above but switching on a field of a ref struct.
        [Fact]
        public void SwitchReadOnlySpanChar_RefStructField()
        {
            var source =
@"using System;
ref struct S
{
    public ReadOnlySpan<char> Chars;
}
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
    }
    static void Test(string str)
    {
        var s = new S() { Chars = str };
        Test(ref s);
    }
    static void Test(ref S s)
    {
        var number = s.Chars switch
        {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3")
                .VerifyIL("C.Test(ref S)",
@"{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0,
                System.ReadOnlySpan<char> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.ReadOnlySpan<char> S.Chars""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_000e:  brfalse.s  IL_0036
  IL_0010:  ldloc.1
  IL_0011:  ldstr      ""string 1""
  IL_0016:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0020:  brtrue.s   IL_003a
  IL_0022:  ldloc.1
  IL_0023:  ldstr      ""string 2""
  IL_0028:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_002d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0032:  brtrue.s   IL_003e
  IL_0034:  br.s       IL_0042
  IL_0036:  ldc.i4.0
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0044
  IL_003a:  ldc.i4.1
  IL_003b:  stloc.0
  IL_003c:  br.s       IL_0044
  IL_003e:  ldc.i4.2
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0044
  IL_0042:  ldc.i4.3
  IL_0043:  stloc.0
  IL_0044:  ldloc.0
  IL_0045:  call       ""void System.Console.WriteLine(int)""
  IL_004a:  ret
}");
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnConstantStringUsingHash()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
        Test(""string 4"");
        Test(""string 5"");
        Test(""string 6"");
        Test(""string 7"");
        Test(""string 8"");
        Test(""string 9"");
    }
    static void Test(ReadOnlySpan<char> chars) 
    {
        var number = chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            ""string 3"" => 3,
            ""string 4"" => 4,
            ""string 5"" => 5,
            ""string 6"" => 6,
            ""string 7"" => 7,
            ""string 8"" => 8,
            _ => 9,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.Test", @"
{
  // Code size      377 (0x179)
  .maxstack  2
  .locals init (int V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""uint <PrivateImplementationDetails>.ComputeReadOnlySpanHash(System.ReadOnlySpan<char>)""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x75b03721
  IL_000d:  bgt.un.s   IL_0047
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x73b033fb
  IL_0015:  bgt.un.s   IL_002f
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0x6ab025d0
  IL_001d:  beq        IL_0137
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x73b033fb
  IL_0028:  beq.s      IL_009c
  IL_002a:  br         IL_016f
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4     0x74b0358e
  IL_0035:  beq.s      IL_00b6
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4     0x75b03721
  IL_003d:  beq        IL_00d0
  IL_0042:  br         IL_016f
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4     0x77b03a47
  IL_004d:  bgt.un.s   IL_006a
  IL_004f:  ldloc.1
  IL_0050:  ldc.i4     0x76b038b4
  IL_0055:  beq        IL_00e7
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4     0x77b03a47
  IL_0060:  beq        IL_00fb
  IL_0065:  br         IL_016f
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0x78b03bda
  IL_0070:  beq        IL_010f
  IL_0075:  ldloc.1
  IL_0076:  ldc.i4     0x79b03d6d
  IL_007b:  beq        IL_0123
  IL_0080:  ldloc.1
  IL_0081:  ldc.i4     0x811c9dc5
  IL_0086:  bne.un     IL_016f
  IL_008b:  ldarga.s   V_0
  IL_008d:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0092:  brfalse    IL_014b
  IL_0097:  br         IL_016f
  IL_009c:  ldarg.0
  IL_009d:  ldstr      ""string 1""
  IL_00a2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00ac:  brtrue     IL_014f
  IL_00b1:  br         IL_016f
  IL_00b6:  ldarg.0
  IL_00b7:  ldstr      ""string 2""
  IL_00bc:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00c1:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00c6:  brtrue     IL_0153
  IL_00cb:  br         IL_016f
  IL_00d0:  ldarg.0
  IL_00d1:  ldstr      ""string 3""
  IL_00d6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00db:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00e0:  brtrue.s   IL_0157
  IL_00e2:  br         IL_016f
  IL_00e7:  ldarg.0
  IL_00e8:  ldstr      ""string 4""
  IL_00ed:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00f2:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00f7:  brtrue.s   IL_015b
  IL_00f9:  br.s       IL_016f
  IL_00fb:  ldarg.0
  IL_00fc:  ldstr      ""string 5""
  IL_0101:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0106:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_010b:  brtrue.s   IL_015f
  IL_010d:  br.s       IL_016f
  IL_010f:  ldarg.0
  IL_0110:  ldstr      ""string 6""
  IL_0115:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_011a:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_011f:  brtrue.s   IL_0163
  IL_0121:  br.s       IL_016f
  IL_0123:  ldarg.0
  IL_0124:  ldstr      ""string 7""
  IL_0129:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_012e:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0133:  brtrue.s   IL_0167
  IL_0135:  br.s       IL_016f
  IL_0137:  ldarg.0
  IL_0138:  ldstr      ""string 8""
  IL_013d:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0142:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0147:  brtrue.s   IL_016b
  IL_0149:  br.s       IL_016f
  IL_014b:  ldc.i4.0
  IL_014c:  stloc.0
  IL_014d:  br.s       IL_0172
  IL_014f:  ldc.i4.1
  IL_0150:  stloc.0
  IL_0151:  br.s       IL_0172
  IL_0153:  ldc.i4.2
  IL_0154:  stloc.0
  IL_0155:  br.s       IL_0172
  IL_0157:  ldc.i4.3
  IL_0158:  stloc.0
  IL_0159:  br.s       IL_0172
  IL_015b:  ldc.i4.4
  IL_015c:  stloc.0
  IL_015d:  br.s       IL_0172
  IL_015f:  ldc.i4.5
  IL_0160:  stloc.0
  IL_0161:  br.s       IL_0172
  IL_0163:  ldc.i4.6
  IL_0164:  stloc.0
  IL_0165:  br.s       IL_0172
  IL_0167:  ldc.i4.7
  IL_0168:  stloc.0
  IL_0169:  br.s       IL_0172
  IL_016b:  ldc.i4.8
  IL_016c:  stloc.0
  IL_016d:  br.s       IL_0172
  IL_016f:  ldc.i4.s   9
  IL_0171:  stloc.0
  IL_0172:  ldloc.0
  IL_0173:  call       ""void System.Console.WriteLine(int)""
  IL_0178:  ret
}");

            compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.Test", @"
{
  // Code size      298 (0x12a)
  .maxstack  2
  .locals init (int V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  brfalse    IL_00fc
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.8
  IL_0010:  bne.un     IL_0120
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldc.i4.7
  IL_0018:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_001d:  ldind.u2
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  ldc.i4.s   49
  IL_0022:  sub
  IL_0023:  switch    (
        IL_004d,
        IL_0067,
        IL_0081,
        IL_0098,
        IL_00ac,
        IL_00c0,
        IL_00d4,
        IL_00e8)
  IL_0048:  br         IL_0120
  IL_004d:  ldarg.0
  IL_004e:  ldstr      ""string 1""
  IL_0053:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0058:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_005d:  brtrue     IL_0100
  IL_0062:  br         IL_0120
  IL_0067:  ldarg.0
  IL_0068:  ldstr      ""string 2""
  IL_006d:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0072:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0077:  brtrue     IL_0104
  IL_007c:  br         IL_0120
  IL_0081:  ldarg.0
  IL_0082:  ldstr      ""string 3""
  IL_0087:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_008c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0091:  brtrue.s   IL_0108
  IL_0093:  br         IL_0120
  IL_0098:  ldarg.0
  IL_0099:  ldstr      ""string 4""
  IL_009e:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a3:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00a8:  brtrue.s   IL_010c
  IL_00aa:  br.s       IL_0120
  IL_00ac:  ldarg.0
  IL_00ad:  ldstr      ""string 5""
  IL_00b2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00b7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00bc:  brtrue.s   IL_0110
  IL_00be:  br.s       IL_0120
  IL_00c0:  ldarg.0
  IL_00c1:  ldstr      ""string 6""
  IL_00c6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00cb:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00d0:  brtrue.s   IL_0114
  IL_00d2:  br.s       IL_0120
  IL_00d4:  ldarg.0
  IL_00d5:  ldstr      ""string 7""
  IL_00da:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00df:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00e4:  brtrue.s   IL_0118
  IL_00e6:  br.s       IL_0120
  IL_00e8:  ldarg.0
  IL_00e9:  ldstr      ""string 8""
  IL_00ee:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00f3:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00f8:  brtrue.s   IL_011c
  IL_00fa:  br.s       IL_0120
  IL_00fc:  ldc.i4.0
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_0123
  IL_0100:  ldc.i4.1
  IL_0101:  stloc.0
  IL_0102:  br.s       IL_0123
  IL_0104:  ldc.i4.2
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_0123
  IL_0108:  ldc.i4.3
  IL_0109:  stloc.0
  IL_010a:  br.s       IL_0123
  IL_010c:  ldc.i4.4
  IL_010d:  stloc.0
  IL_010e:  br.s       IL_0123
  IL_0110:  ldc.i4.5
  IL_0111:  stloc.0
  IL_0112:  br.s       IL_0123
  IL_0114:  ldc.i4.6
  IL_0115:  stloc.0
  IL_0116:  br.s       IL_0123
  IL_0118:  ldc.i4.7
  IL_0119:  stloc.0
  IL_011a:  br.s       IL_0123
  IL_011c:  ldc.i4.8
  IL_011d:  stloc.0
  IL_011e:  br.s       IL_0123
  IL_0120:  ldc.i4.s   9
  IL_0122:  stloc.0
  IL_0123:  ldloc.0
  IL_0124:  call       ""void System.Console.WriteLine(int)""
  IL_0129:  ret
}");
        }

        [Fact]
        public void SwitchStatementReadOnlySpanCharOnConstantStringUsingHash()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
        Test(""string 4"");
        Test(""string 5"");
        Test(""string 6"");
        Test(""string 7"");
        Test(""string 8"");
        Test(""string 9"");
    }
    static void Test(ReadOnlySpan<char> chars) 
    {
        Console.WriteLine(GetResult(chars));
    }
    static int GetResult(ReadOnlySpan<char> chars) 
    {
        switch (chars)
        {
            case """": return 0;
            case ""string 1"": return 1;
            case ""string 2"": return 2;
            case ""string 3"": return 3;
            case ""string 4"": return 4;
            case ""string 5"": return 5;
            case ""string 6"": return 6;
            case ""string 7"": return 7;
            case ""string 8"": return 8;
            default: return 9;
        }
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.GetResult", @"
{
  // Code size      349 (0x15d)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""uint <PrivateImplementationDetails>.ComputeReadOnlySpanHash(System.ReadOnlySpan<char>)""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0x75b03721
  IL_000d:  bgt.un.s   IL_0047
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4     0x73b033fb
  IL_0015:  bgt.un.s   IL_002f
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4     0x6ab025d0
  IL_001d:  beq        IL_0134
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4     0x73b033fb
  IL_0028:  beq.s      IL_009c
  IL_002a:  br         IL_015a
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4     0x74b0358e
  IL_0035:  beq.s      IL_00b6
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4     0x75b03721
  IL_003d:  beq        IL_00d0
  IL_0042:  br         IL_015a
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4     0x77b03a47
  IL_004d:  bgt.un.s   IL_006a
  IL_004f:  ldloc.0
  IL_0050:  ldc.i4     0x76b038b4
  IL_0055:  beq        IL_00e4
  IL_005a:  ldloc.0
  IL_005b:  ldc.i4     0x77b03a47
  IL_0060:  beq        IL_00f8
  IL_0065:  br         IL_015a
  IL_006a:  ldloc.0
  IL_006b:  ldc.i4     0x78b03bda
  IL_0070:  beq        IL_010c
  IL_0075:  ldloc.0
  IL_0076:  ldc.i4     0x79b03d6d
  IL_007b:  beq        IL_0120
  IL_0080:  ldloc.0
  IL_0081:  ldc.i4     0x811c9dc5
  IL_0086:  bne.un     IL_015a
  IL_008b:  ldarga.s   V_0
  IL_008d:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0092:  brfalse    IL_0148
  IL_0097:  br         IL_015a
  IL_009c:  ldarg.0
  IL_009d:  ldstr      ""string 1""
  IL_00a2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00ac:  brtrue     IL_014a
  IL_00b1:  br         IL_015a
  IL_00b6:  ldarg.0
  IL_00b7:  ldstr      ""string 2""
  IL_00bc:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00c1:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00c6:  brtrue     IL_014c
  IL_00cb:  br         IL_015a
  IL_00d0:  ldarg.0
  IL_00d1:  ldstr      ""string 3""
  IL_00d6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00db:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00e0:  brtrue.s   IL_014e
  IL_00e2:  br.s       IL_015a
  IL_00e4:  ldarg.0
  IL_00e5:  ldstr      ""string 4""
  IL_00ea:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00ef:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_00f4:  brtrue.s   IL_0150
  IL_00f6:  br.s       IL_015a
  IL_00f8:  ldarg.0
  IL_00f9:  ldstr      ""string 5""
  IL_00fe:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0103:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0108:  brtrue.s   IL_0152
  IL_010a:  br.s       IL_015a
  IL_010c:  ldarg.0
  IL_010d:  ldstr      ""string 6""
  IL_0112:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0117:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_011c:  brtrue.s   IL_0154
  IL_011e:  br.s       IL_015a
  IL_0120:  ldarg.0
  IL_0121:  ldstr      ""string 7""
  IL_0126:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_012b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0130:  brtrue.s   IL_0156
  IL_0132:  br.s       IL_015a
  IL_0134:  ldarg.0
  IL_0135:  ldstr      ""string 8""
  IL_013a:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_013f:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0144:  brtrue.s   IL_0158
  IL_0146:  br.s       IL_015a
  IL_0148:  ldc.i4.0
  IL_0149:  ret
  IL_014a:  ldc.i4.1
  IL_014b:  ret
  IL_014c:  ldc.i4.2
  IL_014d:  ret
  IL_014e:  ldc.i4.3
  IL_014f:  ret
  IL_0150:  ldc.i4.4
  IL_0151:  ret
  IL_0152:  ldc.i4.5
  IL_0153:  ret
  IL_0154:  ldc.i4.6
  IL_0155:  ret
  IL_0156:  ldc.i4.7
  IL_0157:  ret
  IL_0158:  ldc.i4.8
  IL_0159:  ret
  IL_015a:  ldc.i4.s   9
  IL_015c:  ret
}");

            compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.GetResult", """
{
  // Code size      270 (0x10e)
  .maxstack  2
  .locals init (int V_0,
                char V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       "int System.ReadOnlySpan<char>.Length.get"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brfalse    IL_00f9
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.8
  IL_0010:  bne.un     IL_010b
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldc.i4.7
  IL_0018:  call       "ref readonly char System.ReadOnlySpan<char>.this[int].get"
  IL_001d:  ldind.u2
  IL_001e:  stloc.1
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.s   49
  IL_0022:  sub
  IL_0023:  switch    (
        IL_004d,
        IL_0067,
        IL_0081,
        IL_0095,
        IL_00a9,
        IL_00bd,
        IL_00d1,
        IL_00e5)
  IL_0048:  br         IL_010b
  IL_004d:  ldarg.0
  IL_004e:  ldstr      "string 1"
  IL_0053:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0058:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_005d:  brtrue     IL_00fb
  IL_0062:  br         IL_010b
  IL_0067:  ldarg.0
  IL_0068:  ldstr      "string 2"
  IL_006d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0072:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0077:  brtrue     IL_00fd
  IL_007c:  br         IL_010b
  IL_0081:  ldarg.0
  IL_0082:  ldstr      "string 3"
  IL_0087:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_008c:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0091:  brtrue.s   IL_00ff
  IL_0093:  br.s       IL_010b
  IL_0095:  ldarg.0
  IL_0096:  ldstr      "string 4"
  IL_009b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00a0:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_00a5:  brtrue.s   IL_0101
  IL_00a7:  br.s       IL_010b
  IL_00a9:  ldarg.0
  IL_00aa:  ldstr      "string 5"
  IL_00af:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00b4:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_00b9:  brtrue.s   IL_0103
  IL_00bb:  br.s       IL_010b
  IL_00bd:  ldarg.0
  IL_00be:  ldstr      "string 6"
  IL_00c3:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00c8:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_00cd:  brtrue.s   IL_0105
  IL_00cf:  br.s       IL_010b
  IL_00d1:  ldarg.0
  IL_00d2:  ldstr      "string 7"
  IL_00d7:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00dc:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_00e1:  brtrue.s   IL_0107
  IL_00e3:  br.s       IL_010b
  IL_00e5:  ldarg.0
  IL_00e6:  ldstr      "string 8"
  IL_00eb:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_00f0:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_00f5:  brtrue.s   IL_0109
  IL_00f7:  br.s       IL_010b
  IL_00f9:  ldc.i4.0
  IL_00fa:  ret
  IL_00fb:  ldc.i4.1
  IL_00fc:  ret
  IL_00fd:  ldc.i4.2
  IL_00fe:  ret
  IL_00ff:  ldc.i4.3
  IL_0100:  ret
  IL_0101:  ldc.i4.4
  IL_0102:  ret
  IL_0103:  ldc.i4.5
  IL_0104:  ret
  IL_0105:  ldc.i4.6
  IL_0106:  ret
  IL_0107:  ldc.i4.7
  IL_0108:  ret
  IL_0109:  ldc.i4.8
  IL_010a:  ret
  IL_010b:  ldc.i4.s   9
  IL_010d:  ret
}
""");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchReadOnlySpanCharOnConstantStringAndOtherPatterns()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
    }
    static void Test(ReadOnlySpan<char> chars) 
    {
        var number = chars switch {
            { Length: 0 } => 0,
            ""string 1"" and [..,'1'] => 1,
            { Length: 8 } and ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3")
                .VerifyIL("C.Test", @"
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  brfalse.s  IL_0046
  IL_000b:  ldarg.0
  IL_000c:  ldstr      ""string 1""
  IL_0011:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0016:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_001b:  brfalse.s  IL_002e
  IL_001d:  ldarga.s   V_0
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  sub
  IL_0022:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0027:  ldind.u2
  IL_0028:  ldc.i4.s   49
  IL_002a:  beq.s      IL_004a
  IL_002c:  br.s       IL_0052
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.8
  IL_0030:  bne.un.s   IL_0052
  IL_0032:  ldarg.0
  IL_0033:  ldstr      ""string 2""
  IL_0038:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_003d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0042:  brtrue.s   IL_004e
  IL_0044:  br.s       IL_0052
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_0054
  IL_004a:  ldc.i4.1
  IL_004b:  stloc.0
  IL_004c:  br.s       IL_0054
  IL_004e:  ldc.i4.2
  IL_004f:  stloc.0
  IL_0050:  br.s       IL_0054
  IL_0052:  ldc.i4.3
  IL_0053:  stloc.0
  IL_0054:  ldloc.0
  IL_0055:  call       ""void System.Console.WriteLine(int)""
  IL_005a:  ret
}");
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnConstantStringInOrAndAndNot()
        {
            var source =
    @"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
    }
    static void Test(ReadOnlySpan<char> chars)
    {
        Console.WriteLine(""or: "" + (chars is ""string 1"" or ""string 2""));
        Console.WriteLine(""and: "" + (chars is ""string 1"" and { Length: 7 }));
        Console.WriteLine(""not: "" + (chars is not ""string 1""));
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"or: False
and: False
not: True
or: True
and: False
not: False
or: True
and: False
not: True
or: False
and: False
not: True")
                .VerifyIL("C.Test", """
{
  // Code size      167 (0xa7)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldstr      "string 1"
  IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_000c:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0011:  brtrue.s   IL_0027
  IL_0013:  ldarg.0
  IL_0014:  ldstr      "string 2"
  IL_0019:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_001e:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0023:  brtrue.s   IL_0027
  IL_0025:  br.s       IL_002b
  IL_0027:  ldc.i4.1
  IL_0028:  stloc.0
  IL_0029:  br.s       IL_002d
  IL_002b:  ldc.i4.0
  IL_002c:  stloc.0
  IL_002d:  ldstr      "or: "
  IL_0032:  ldloca.s   V_0
  IL_0034:  call       "string bool.ToString()"
  IL_0039:  call       "string string.Concat(string, string)"
  IL_003e:  call       "void System.Console.WriteLine(string)"
  IL_0043:  nop
  IL_0044:  ldstr      "and: "
  IL_0049:  ldarg.0
  IL_004a:  ldstr      "string 1"
  IL_004f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0054:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0059:  brfalse.s  IL_0067
  IL_005b:  ldarga.s   V_0
  IL_005d:  call       "int System.ReadOnlySpan<char>.Length.get"
  IL_0062:  ldc.i4.7
  IL_0063:  ceq
  IL_0065:  br.s       IL_0068
  IL_0067:  ldc.i4.0
  IL_0068:  stloc.0
  IL_0069:  ldloca.s   V_0
  IL_006b:  call       "string bool.ToString()"
  IL_0070:  call       "string string.Concat(string, string)"
  IL_0075:  call       "void System.Console.WriteLine(string)"
  IL_007a:  nop
  IL_007b:  ldstr      "not: "
  IL_0080:  ldarg.0
  IL_0081:  ldstr      "string 1"
  IL_0086:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_008b:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
  IL_0090:  ldc.i4.0
  IL_0091:  ceq
  IL_0093:  stloc.0
  IL_0094:  ldloca.s   V_0
  IL_0096:  call       "string bool.ToString()"
  IL_009b:  call       "string string.Concat(string, string)"
  IL_00a0:  call       "void System.Console.WriteLine(string)"
  IL_00a5:  nop
  IL_00a6:  ret
}
""");
        }

        [Fact]
        public void RecursivePatternMatchReadOnlySpanCharOnConstantString()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test(new S { Span = ""string 1"", Prop = true });
        Test(new S { Span = ""string 1"", Prop = false });
        Test(new S { Span = ""string 2"", Prop = true });
        Test(new S { Span = ""string 2"", Prop = false });
    }
    static void Test(S s) => Console.WriteLine(s is { Prop: true, Span: ""string 1"" and { Length: 8 } });
}

ref struct S
{
    public ReadOnlySpan<char> Span { get; set; }
    public bool Prop { get; set; }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: @"True
False
False
False")
                .VerifyIL("C.Test", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.ReadOnlySpan<char> V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool S.Prop.get""
  IL_0007:  brfalse.s  IL_002f
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""readonly System.ReadOnlySpan<char> S.Span.get""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldstr      ""string 1""
  IL_0017:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0021:  brfalse.s  IL_002f
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_002a:  ldc.i4.8
  IL_002b:  ceq
  IL_002d:  br.s       IL_0030
  IL_002f:  ldc.i4.0
  IL_0030:  call       ""void System.Console.WriteLine(bool)""
  IL_0035:  nop
  IL_0036:  ret
}");
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnConstantStringMissingMemoryExtensions()
        {
            var source =
@"
using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars is """";
}
";
            CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (5,57): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //     static bool M(ReadOnlySpan<char> chars) => chars is "";
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(5, 57),
                    // (5,57): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //     static bool M(ReadOnlySpan<char> chars) => chars is "";
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(5, 57));
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnConstantStringMissingMemoryExtensions()
        {
            var source =
@"
using System;
class C
{
    static int M(ReadOnlySpan<char> chars) 
    {
        return chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
    }
}
";
            CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "" => 0,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(8, 13),
                    // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "" => 0,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(8, 13),
                    // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "string 1" => 1,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 1""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(9, 13),
                    // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "string 1" => 1,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 1""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(9, 13),
                    // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "string 2" => 2,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 2""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(10, 13),
                    // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "string 2" => 2,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 2""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(10, 13));
        }

        [Fact]
        public void PatternOrSwitchReadOnlySpanChar_MissingLengthAndIndexer()
        {
            var sourceA =
@"namespace System
{
    public ref struct ReadOnlySpan<T>
    {
        public ReadOnlySpan(T[] array) { }
    }
    public static class MemoryExtensions
    {
        public static ReadOnlySpan<char> AsSpan(string s) => default;
        public static bool SequenceEqual<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) => false;
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var s = new ReadOnlySpan<char>(new char[0]);
        _ = s is ""str"";
        _ = s is { Length: 0 } and """";
        _ = s switch { ""str"" => 1, _ => 0 };
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (7,18): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.get_Length'
                //         _ = s is "str";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""str""").WithArguments("System.ReadOnlySpan`1", "get_Length").WithLocation(7, 18),
                // (8,20): error CS0117: 'ReadOnlySpan<char>' does not contain a definition for 'Length'
                //         _ = s is { Length: 0 } and "";
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Length").WithArguments("System.ReadOnlySpan<char>", "Length").WithLocation(8, 20),
                // (8,36): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.get_Length'
                //         _ = s is { Length: 0 } and "";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.ReadOnlySpan`1", "get_Length").WithLocation(8, 36),
                // (9,24): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.get_Length'
                //         _ = s switch { "str" => 1, _ => 0 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""str""").WithArguments("System.ReadOnlySpan`1", "get_Length").WithLocation(9, 24));
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnConstantStringCSharp10()
        {
            var source =
@"
using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars is """";
    static void Main()
    {
        Console.WriteLine(M(new ReadOnlySpan<char>(null)));
        Console.WriteLine(M((string)null));
        Console.WriteLine(M(""""));
        Console.WriteLine(M(""str""));
    }
}
";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,57): error CS8936: Feature 'pattern matching ReadOnly/Span<char> on constant string' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static bool M(ReadOnlySpan<char> chars) => chars is "";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""").WithArguments("pattern matching ReadOnly/Span<char> on constant string", "11.0").WithLocation(5, 57));

            comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
True
False");
            verifier.VerifyIL("C.M",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      """"
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnConstantStringCSharp10()
        {
            var source =
@"using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch { """" => true, _ => false };
    static void Main()
    {
        Console.WriteLine(M(new ReadOnlySpan<char>(null)));
        Console.WriteLine(M((string)null));
        Console.WriteLine(M(""""));
        Console.WriteLine(M(""str""));
    }
}
";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (4,63): error CS8936: Feature 'pattern matching ReadOnly/Span<char> on constant string' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static bool M(ReadOnlySpan<char> chars) => chars switch { "" => true, _ => false };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""").WithArguments("pattern matching ReadOnly/Span<char> on constant string", "11.0").WithLocation(4, 63));

            comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
True
False");
            verifier.VerifyIL("C.M",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      """"
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0010:  brfalse.s  IL_0016
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0018
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void PatternMatchReadOnlySpanCharOnNull_01()
        {
            var source =
@"
using System;
class C
{
    static bool M1(ReadOnlySpan<char> chars) => chars is null;
    static bool M2(ReadOnlySpan<char> chars) => chars is default;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,58): error CS9133: A constant value of type 'ReadOnlySpan<char>' is expected
                //     static bool M1(ReadOnlySpan<char> chars) => chars is null;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "null").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 58),
                // (6,58): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //     static bool M2(ReadOnlySpan<char> chars) => chars is default;
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(6, 58));
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnNull_02()
        {
            var source =
@"using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars is (object)null;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,57): error CS0266: Cannot implicitly convert type 'object' to 'System.ReadOnlySpan<char>'. An explicit conversion exists (are you missing a cast?)
                //     static bool M(ReadOnlySpan<char> chars) => chars is (object)null;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "System.ReadOnlySpan<char>").WithLocation(4, 57));
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnNull_03()
        {
            var source =
@"using System;
class C
{
    static bool M1(ReadOnlySpan<char> chars) => chars is (string)null;
    static bool M2(ReadOnlySpan<char> chars) => chars is default(string);
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,58): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //     static bool M1(ReadOnlySpan<char> chars) => chars is (string)null;
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "(string)null").WithArguments("System.ReadOnlySpan<char>").WithLocation(4, 58),
                // (5,58): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //     static bool M2(ReadOnlySpan<char> chars) => chars is default(string);
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "default(string)").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 58));
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnNull_04()
        {
            var source =
@"using System;
class C
{
    const string NullString = null;
    static bool M(ReadOnlySpan<char> chars) => chars is NullString;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,57): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //     static bool M(ReadOnlySpan<char> chars) => chars is NullString;
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "NullString").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 57));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchReadOnlySpanCharOnNull_01()
        {
            var source =
@"
using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch { null => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (5,63): error CS9133: A constant value of type 'ReadOnlySpan<char>' is expected
                    //     static bool M(ReadOnlySpan<char> chars) => chars switch { null => true, _ => false };
                    Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "null").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 63)
                );
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnNull_02()
        {
            var source =
@"using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch { (object)null => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,63): error CS0266: Cannot implicitly convert type 'object' to 'System.ReadOnlySpan<char>'. An explicit conversion exists (are you missing a cast?)
                //     static bool M(ReadOnlySpan<char> chars) => chars switch { (object)null => true, _ => false };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "System.ReadOnlySpan<char>").WithLocation(4, 63));
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnNull_03()
        {
            var source =
@"using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch { (string)null => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,63): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //     static bool M(ReadOnlySpan<char> chars) => chars switch { (string)null => true, _ => false };
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "(string)null").WithArguments("System.ReadOnlySpan<char>").WithLocation(4, 63));
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnNull_04()
        {
            var source =
@"using System;
class C
{
    const string NullString = null;
    static bool M(ReadOnlySpan<char> chars) => chars switch { NullString => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,63): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //     static bool M(ReadOnlySpan<char> chars) => chars switch { NullString => true, _ => false };
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "NullString").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 63));
        }

        [Fact]
        public void MatchReadOnlySpanCharOnImpossiblePatterns()
        {
            var source =
@"
using System;
class C
{
    static void M(ReadOnlySpan<char> chars)
    {
        _ = chars is """" and "" "";
        _ = chars is """" and not """";
        _ = chars is """" and ("" "" or not """");
    }
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (7,13): error CS8518: An expression of type 'ReadOnlySpan<char>' can never match the provided pattern.
                    //         _ = chars is "" and " ";
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and "" """).WithArguments("System.ReadOnlySpan<char>").WithLocation(7, 13),
                    // (8,13): error CS8518: An expression of type 'ReadOnlySpan<char>' can never match the provided pattern.
                    //         _ = chars is "" and not "";
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and not """"").WithArguments("System.ReadOnlySpan<char>").WithLocation(8, 13),
                    // (9,13): error CS8518: An expression of type 'ReadOnlySpan<char>' can never match the provided pattern.
                    //         _ = chars is "" and (" " or not "");
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and ("" "" or not """")").WithArguments("System.ReadOnlySpan<char>").WithLocation(9, 13));
        }

        [Fact]
        public void PatternMatchReadOnlySpanCharOnPossiblePatterns()
        {
            var source =
@"
using System;
class C
{
    static void Main()
    {
        Test("""");
        Test("" "");
        Test(""  "");
    }
    static void Test(ReadOnlySpan<char> chars)
    {
        Console.WriteLine(""1."" + (chars is """" and not "" ""));
        Console.WriteLine(""2."" + (chars is """" and ("" "" or """")));
        Console.WriteLine(""3."" + (chars is """" or """"));
        Console.WriteLine(""4."" + (chars is """" or not """"));
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (13,55): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("1." + (chars is "" and not " "));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" """).WithLocation(13, 55),
                    // (14,52): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("2." + (chars is "" and (" " or "")));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" """).WithLocation(14, 52),
                    // (14,52): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("2." + (chars is "" and (" " or "")));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" "" or """"").WithLocation(14, 52),
                    // (15,50): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("3." + (chars is "" or ""));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @"""""").WithLocation(15, 50),
                    // (16,35): warning CS8794: An expression of type 'ReadOnlySpan<char>' always matches the provided pattern.
                    //         Console.WriteLine("4." + (chars is "" or not ""));
                    Diagnostic(ErrorCode.WRN_IsPatternAlways, @"chars is """" or not """"").WithArguments("System.ReadOnlySpan<char>").WithLocation(16, 35));
            CompileAndVerify(compilation, expectedOutput: @"1.True
2.True
3.True
4.True
1.False
2.False
3.False
4.True
1.False
2.False
3.False
4.True")
                .VerifyIL("C.Test", @"
{
  // Code size      159 (0x9f)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  ldstr      ""1.""
  IL_0005:  ldarg.0
  IL_0006:  ldstr      """"
  IL_000b:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0010:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""string bool.ToString()""
  IL_001d:  call       ""string string.Concat(string, string)""
  IL_0022:  call       ""void System.Console.WriteLine(string)""
  IL_0027:  ldstr      ""2.""
  IL_002c:  ldarg.0
  IL_002d:  ldstr      """"
  IL_0032:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0037:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_003c:  stloc.0
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""string bool.ToString()""
  IL_0044:  call       ""string string.Concat(string, string)""
  IL_0049:  call       ""void System.Console.WriteLine(string)""
  IL_004e:  ldstr      ""3.""
  IL_0053:  ldarg.0
  IL_0054:  ldstr      """"
  IL_0059:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_005e:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0063:  stloc.0
  IL_0064:  ldloca.s   V_0
  IL_0066:  call       ""string bool.ToString()""
  IL_006b:  call       ""string string.Concat(string, string)""
  IL_0070:  call       ""void System.Console.WriteLine(string)""
  IL_0075:  ldarg.0
  IL_0076:  ldstr      """"
  IL_007b:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0080:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0085:  pop
  IL_0086:  ldc.i4.1
  IL_0087:  stloc.0
  IL_0088:  ldstr      ""4.""
  IL_008d:  ldloca.s   V_0
  IL_008f:  call       ""string bool.ToString()""
  IL_0094:  call       ""string string.Concat(string, string)""
  IL_0099:  call       ""void System.Console.WriteLine(string)""
  IL_009e:  ret
}");
        }

        [Fact]
        public void SwitchReadOnlySpanCharOnDuplicateString()
        {
            var source =
@"
using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch {
        """" => true,
        """" => false,
        _ => false,
    };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (7,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                    //         "" => false,
                    Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""""").WithLocation(7, 9));
        }

        [Fact]
        public void PatternMatchSpanCharOnConstantString()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test(""test string"".ToArray());
        Test(""test string? I think not!"".ToArray());
        Test(""WrongString"".ToArray());
    }
    static void Test(Span<char> chars) => Console.WriteLine(chars is ""test string"");
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"False
True
False
False")
                .VerifyIL("C.Test", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""test string""
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0010:  call       ""void System.Console.WriteLine(bool)""
  IL_0015:  nop
  IL_0016:  ret
}");
        }

        [Fact]
        public void SwitchSpanCharOnConstantString()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test(""String 1"".ToArray());
        Test(""string 1"".ToArray());
        Test(""string 2"".ToArray());
        Test(""STRING 2"".ToArray());
        Test(""string 3"".ToArray());
    }
    static void Test(Span<char> chars) 
    {
        var number = chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""STRING 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
3
1
3
2
3")
                .VerifyIL("C.Test", @"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<char>.Length.get""
  IL_0007:  brfalse.s  IL_002f
  IL_0009:  ldarg.0
  IL_000a:  ldstr      ""string 1""
  IL_000f:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0014:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0019:  brtrue.s   IL_0033
  IL_001b:  ldarg.0
  IL_001c:  ldstr      ""STRING 2""
  IL_0021:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0026:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_002b:  brtrue.s   IL_0037
  IL_002d:  br.s       IL_003b
  IL_002f:  ldc.i4.0
  IL_0030:  stloc.0
  IL_0031:  br.s       IL_003d
  IL_0033:  ldc.i4.1
  IL_0034:  stloc.0
  IL_0035:  br.s       IL_003d
  IL_0037:  ldc.i4.2
  IL_0038:  stloc.0
  IL_0039:  br.s       IL_003d
  IL_003b:  ldc.i4.3
  IL_003c:  stloc.0
  IL_003d:  ldloc.0
  IL_003e:  call       ""void System.Console.WriteLine(int)""
  IL_0043:  ret
}");
        }

        // Similar to above but switching on a local value rather than a parameter.
        [Fact]
        public void SwitchSpanChar_Local()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static void Main()
    {
        Span<char> chars = ""string 2"".ToArray();
        var number = chars switch
        {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"2")
                .VerifyIL("C.Main",
@"{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (System.Span<char> V_0, //chars
                int V_1)
  IL_0000:  ldstr      ""string 2""
  IL_0005:  call       ""char[] System.Linq.Enumerable.ToArray<char>(System.Collections.Generic.IEnumerable<char>)""
  IL_000a:  call       ""System.Span<char> System.Span<char>.op_Implicit(char[])""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""int System.Span<char>.Length.get""
  IL_0017:  brfalse.s  IL_003f
  IL_0019:  ldloc.0
  IL_001a:  ldstr      ""string 1""
  IL_001f:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0024:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0029:  brtrue.s   IL_0043
  IL_002b:  ldloc.0
  IL_002c:  ldstr      ""string 2""
  IL_0031:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0036:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_003b:  brtrue.s   IL_0047
  IL_003d:  br.s       IL_004b
  IL_003f:  ldc.i4.0
  IL_0040:  stloc.1
  IL_0041:  br.s       IL_004d
  IL_0043:  ldc.i4.1
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_004d
  IL_0047:  ldc.i4.2
  IL_0048:  stloc.1
  IL_0049:  br.s       IL_004d
  IL_004b:  ldc.i4.3
  IL_004c:  stloc.1
  IL_004d:  ldloc.1
  IL_004e:  call       ""void System.Console.WriteLine(int)""
  IL_0053:  ret
}");
        }

        // Similar to above but switching on a field of a ref struct.
        [Fact]
        public void SwitchSpanChar_RefStructField()
        {
            var source =
@"using System;
using System.Linq;
ref struct S
{
    public Span<char> Chars;
}
class C
{
    static void Main()
    {
        Test("""");
        Test(""string 1"");
        Test(""string 2"");
        Test(""string 3"");
    }
    static void Test(string str)
    {
        var s = new S() { Chars = str.ToArray() };
        Test(ref s);
    }
    static void Test(ref S s)
    {
        var number = s.Chars switch
        {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3")
                .VerifyIL("C.Test(ref S)",
@"{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0,
                System.Span<char> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Span<char> S.Chars""
  IL_0006:  stloc.1
  IL_0007:  ldloca.s   V_1
  IL_0009:  call       ""int System.Span<char>.Length.get""
  IL_000e:  brfalse.s  IL_0036
  IL_0010:  ldloc.1
  IL_0011:  ldstr      ""string 1""
  IL_0016:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0020:  brtrue.s   IL_003a
  IL_0022:  ldloc.1
  IL_0023:  ldstr      ""string 2""
  IL_0028:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_002d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0032:  brtrue.s   IL_003e
  IL_0034:  br.s       IL_0042
  IL_0036:  ldc.i4.0
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0044
  IL_003a:  ldc.i4.1
  IL_003b:  stloc.0
  IL_003c:  br.s       IL_0044
  IL_003e:  ldc.i4.2
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0044
  IL_0042:  ldc.i4.3
  IL_0043:  stloc.0
  IL_0044:  ldloc.0
  IL_0045:  call       ""void System.Console.WriteLine(int)""
  IL_004a:  ret
}");
        }

        [Fact]
        public void SwitchSpanCharOnConstantStringUsingHash()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test(""string 1"".ToArray());
        Test(""string 2"".ToArray());
        Test(""string 3"".ToArray());
        Test(""string 4"".ToArray());
        Test(""string 5"".ToArray());
        Test(""string 6"".ToArray());
        Test(""string 7"".ToArray());
        Test(""string 8"".ToArray());
        Test(""string 9"".ToArray());
    }
    static void Test(Span<char> chars) 
    {
        var number = chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            ""string 3"" => 3,
            ""string 4"" => 4,
            ""string 5"" => 5,
            ""string 6"" => 6,
            ""string 7"" => 7,
            ""string 8"" => 8,
            _ => 9,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.Test", @"
{
  // Code size      377 (0x179)
  .maxstack  2
  .locals init (int V_0,
                uint V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""uint <PrivateImplementationDetails>.ComputeSpanHash(System.Span<char>)""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4     0x75b03721
  IL_000d:  bgt.un.s   IL_0047
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4     0x73b033fb
  IL_0015:  bgt.un.s   IL_002f
  IL_0017:  ldloc.1
  IL_0018:  ldc.i4     0x6ab025d0
  IL_001d:  beq        IL_0137
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4     0x73b033fb
  IL_0028:  beq.s      IL_009c
  IL_002a:  br         IL_016f
  IL_002f:  ldloc.1
  IL_0030:  ldc.i4     0x74b0358e
  IL_0035:  beq.s      IL_00b6
  IL_0037:  ldloc.1
  IL_0038:  ldc.i4     0x75b03721
  IL_003d:  beq        IL_00d0
  IL_0042:  br         IL_016f
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4     0x77b03a47
  IL_004d:  bgt.un.s   IL_006a
  IL_004f:  ldloc.1
  IL_0050:  ldc.i4     0x76b038b4
  IL_0055:  beq        IL_00e7
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4     0x77b03a47
  IL_0060:  beq        IL_00fb
  IL_0065:  br         IL_016f
  IL_006a:  ldloc.1
  IL_006b:  ldc.i4     0x78b03bda
  IL_0070:  beq        IL_010f
  IL_0075:  ldloc.1
  IL_0076:  ldc.i4     0x79b03d6d
  IL_007b:  beq        IL_0123
  IL_0080:  ldloc.1
  IL_0081:  ldc.i4     0x811c9dc5
  IL_0086:  bne.un     IL_016f
  IL_008b:  ldarga.s   V_0
  IL_008d:  call       ""int System.Span<char>.Length.get""
  IL_0092:  brfalse    IL_014b
  IL_0097:  br         IL_016f
  IL_009c:  ldarg.0
  IL_009d:  ldstr      ""string 1""
  IL_00a2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00ac:  brtrue     IL_014f
  IL_00b1:  br         IL_016f
  IL_00b6:  ldarg.0
  IL_00b7:  ldstr      ""string 2""
  IL_00bc:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00c1:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00c6:  brtrue     IL_0153
  IL_00cb:  br         IL_016f
  IL_00d0:  ldarg.0
  IL_00d1:  ldstr      ""string 3""
  IL_00d6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00db:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00e0:  brtrue.s   IL_0157
  IL_00e2:  br         IL_016f
  IL_00e7:  ldarg.0
  IL_00e8:  ldstr      ""string 4""
  IL_00ed:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00f2:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00f7:  brtrue.s   IL_015b
  IL_00f9:  br.s       IL_016f
  IL_00fb:  ldarg.0
  IL_00fc:  ldstr      ""string 5""
  IL_0101:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0106:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_010b:  brtrue.s   IL_015f
  IL_010d:  br.s       IL_016f
  IL_010f:  ldarg.0
  IL_0110:  ldstr      ""string 6""
  IL_0115:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_011a:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_011f:  brtrue.s   IL_0163
  IL_0121:  br.s       IL_016f
  IL_0123:  ldarg.0
  IL_0124:  ldstr      ""string 7""
  IL_0129:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_012e:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0133:  brtrue.s   IL_0167
  IL_0135:  br.s       IL_016f
  IL_0137:  ldarg.0
  IL_0138:  ldstr      ""string 8""
  IL_013d:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0142:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0147:  brtrue.s   IL_016b
  IL_0149:  br.s       IL_016f
  IL_014b:  ldc.i4.0
  IL_014c:  stloc.0
  IL_014d:  br.s       IL_0172
  IL_014f:  ldc.i4.1
  IL_0150:  stloc.0
  IL_0151:  br.s       IL_0172
  IL_0153:  ldc.i4.2
  IL_0154:  stloc.0
  IL_0155:  br.s       IL_0172
  IL_0157:  ldc.i4.3
  IL_0158:  stloc.0
  IL_0159:  br.s       IL_0172
  IL_015b:  ldc.i4.4
  IL_015c:  stloc.0
  IL_015d:  br.s       IL_0172
  IL_015f:  ldc.i4.5
  IL_0160:  stloc.0
  IL_0161:  br.s       IL_0172
  IL_0163:  ldc.i4.6
  IL_0164:  stloc.0
  IL_0165:  br.s       IL_0172
  IL_0167:  ldc.i4.7
  IL_0168:  stloc.0
  IL_0169:  br.s       IL_0172
  IL_016b:  ldc.i4.8
  IL_016c:  stloc.0
  IL_016d:  br.s       IL_0172
  IL_016f:  ldc.i4.s   9
  IL_0171:  stloc.0
  IL_0172:  ldloc.0
  IL_0173:  call       ""void System.Console.WriteLine(int)""
  IL_0178:  ret
}");

            compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe);
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.Test", @"
{
  // Code size      298 (0x12a)
  .maxstack  2
  .locals init (int V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<char>.Length.get""
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  brfalse    IL_00fc
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.8
  IL_0010:  bne.un     IL_0120
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldc.i4.7
  IL_0018:  call       ""ref char System.Span<char>.this[int].get""
  IL_001d:  ldind.u2
  IL_001e:  stloc.2
  IL_001f:  ldloc.2
  IL_0020:  ldc.i4.s   49
  IL_0022:  sub
  IL_0023:  switch    (
        IL_004d,
        IL_0067,
        IL_0081,
        IL_0098,
        IL_00ac,
        IL_00c0,
        IL_00d4,
        IL_00e8)
  IL_0048:  br         IL_0120
  IL_004d:  ldarg.0
  IL_004e:  ldstr      ""string 1""
  IL_0053:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0058:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_005d:  brtrue     IL_0100
  IL_0062:  br         IL_0120
  IL_0067:  ldarg.0
  IL_0068:  ldstr      ""string 2""
  IL_006d:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0072:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0077:  brtrue     IL_0104
  IL_007c:  br         IL_0120
  IL_0081:  ldarg.0
  IL_0082:  ldstr      ""string 3""
  IL_0087:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_008c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0091:  brtrue.s   IL_0108
  IL_0093:  br         IL_0120
  IL_0098:  ldarg.0
  IL_0099:  ldstr      ""string 4""
  IL_009e:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a3:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00a8:  brtrue.s   IL_010c
  IL_00aa:  br.s       IL_0120
  IL_00ac:  ldarg.0
  IL_00ad:  ldstr      ""string 5""
  IL_00b2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00b7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00bc:  brtrue.s   IL_0110
  IL_00be:  br.s       IL_0120
  IL_00c0:  ldarg.0
  IL_00c1:  ldstr      ""string 6""
  IL_00c6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00cb:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00d0:  brtrue.s   IL_0114
  IL_00d2:  br.s       IL_0120
  IL_00d4:  ldarg.0
  IL_00d5:  ldstr      ""string 7""
  IL_00da:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00df:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00e4:  brtrue.s   IL_0118
  IL_00e6:  br.s       IL_0120
  IL_00e8:  ldarg.0
  IL_00e9:  ldstr      ""string 8""
  IL_00ee:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00f3:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00f8:  brtrue.s   IL_011c
  IL_00fa:  br.s       IL_0120
  IL_00fc:  ldc.i4.0
  IL_00fd:  stloc.0
  IL_00fe:  br.s       IL_0123
  IL_0100:  ldc.i4.1
  IL_0101:  stloc.0
  IL_0102:  br.s       IL_0123
  IL_0104:  ldc.i4.2
  IL_0105:  stloc.0
  IL_0106:  br.s       IL_0123
  IL_0108:  ldc.i4.3
  IL_0109:  stloc.0
  IL_010a:  br.s       IL_0123
  IL_010c:  ldc.i4.4
  IL_010d:  stloc.0
  IL_010e:  br.s       IL_0123
  IL_0110:  ldc.i4.5
  IL_0111:  stloc.0
  IL_0112:  br.s       IL_0123
  IL_0114:  ldc.i4.6
  IL_0115:  stloc.0
  IL_0116:  br.s       IL_0123
  IL_0118:  ldc.i4.7
  IL_0119:  stloc.0
  IL_011a:  br.s       IL_0123
  IL_011c:  ldc.i4.8
  IL_011d:  stloc.0
  IL_011e:  br.s       IL_0123
  IL_0120:  ldc.i4.s   9
  IL_0122:  stloc.0
  IL_0123:  ldloc.0
  IL_0124:  call       ""void System.Console.WriteLine(int)""
  IL_0129:  ret
}");
        }

        [Fact]
        public void SwitchStatementSpanCharOnConstantStringUsingHash()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test(""string 1"".ToArray());
        Test(""string 2"".ToArray());
        Test(""string 3"".ToArray());
        Test(""string 4"".ToArray());
        Test(""string 5"".ToArray());
        Test(""string 6"".ToArray());
        Test(""string 7"".ToArray());
        Test(""string 8"".ToArray());
        Test(""string 9"".ToArray());
    }
    static void Test(Span<char> chars) 
    {
        Console.WriteLine(GetResult(chars));
    }
    static int GetResult(Span<char> chars) 
    {
        switch (chars)
        {
            case """": return 0;
            case ""string 1"": return 1;
            case ""string 2"": return 2;
            case ""string 3"": return 3;
            case ""string 4"": return 4;
            case ""string 5"": return 5;
            case ""string 6"": return 6;
            case ""string 7"": return 7;
            case ""string 8"": return 8;
            default: return 9;
        }
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.RegularPreview.WithDisableLengthBasedSwitch());
            compilation.VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.GetResult", @"
{
  // Code size      349 (0x15d)
  .maxstack  2
  .locals init (uint V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""uint <PrivateImplementationDetails>.ComputeSpanHash(System.Span<char>)""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4     0x75b03721
  IL_000d:  bgt.un.s   IL_0047
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4     0x73b033fb
  IL_0015:  bgt.un.s   IL_002f
  IL_0017:  ldloc.0
  IL_0018:  ldc.i4     0x6ab025d0
  IL_001d:  beq        IL_0134
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4     0x73b033fb
  IL_0028:  beq.s      IL_009c
  IL_002a:  br         IL_015a
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4     0x74b0358e
  IL_0035:  beq.s      IL_00b6
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4     0x75b03721
  IL_003d:  beq        IL_00d0
  IL_0042:  br         IL_015a
  IL_0047:  ldloc.0
  IL_0048:  ldc.i4     0x77b03a47
  IL_004d:  bgt.un.s   IL_006a
  IL_004f:  ldloc.0
  IL_0050:  ldc.i4     0x76b038b4
  IL_0055:  beq        IL_00e4
  IL_005a:  ldloc.0
  IL_005b:  ldc.i4     0x77b03a47
  IL_0060:  beq        IL_00f8
  IL_0065:  br         IL_015a
  IL_006a:  ldloc.0
  IL_006b:  ldc.i4     0x78b03bda
  IL_0070:  beq        IL_010c
  IL_0075:  ldloc.0
  IL_0076:  ldc.i4     0x79b03d6d
  IL_007b:  beq        IL_0120
  IL_0080:  ldloc.0
  IL_0081:  ldc.i4     0x811c9dc5
  IL_0086:  bne.un     IL_015a
  IL_008b:  ldarga.s   V_0
  IL_008d:  call       ""int System.Span<char>.Length.get""
  IL_0092:  brfalse    IL_0148
  IL_0097:  br         IL_015a
  IL_009c:  ldarg.0
  IL_009d:  ldstr      ""string 1""
  IL_00a2:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a7:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00ac:  brtrue     IL_014a
  IL_00b1:  br         IL_015a
  IL_00b6:  ldarg.0
  IL_00b7:  ldstr      ""string 2""
  IL_00bc:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00c1:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00c6:  brtrue     IL_014c
  IL_00cb:  br         IL_015a
  IL_00d0:  ldarg.0
  IL_00d1:  ldstr      ""string 3""
  IL_00d6:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00db:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00e0:  brtrue.s   IL_014e
  IL_00e2:  br.s       IL_015a
  IL_00e4:  ldarg.0
  IL_00e5:  ldstr      ""string 4""
  IL_00ea:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00ef:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00f4:  brtrue.s   IL_0150
  IL_00f6:  br.s       IL_015a
  IL_00f8:  ldarg.0
  IL_00f9:  ldstr      ""string 5""
  IL_00fe:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0103:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0108:  brtrue.s   IL_0152
  IL_010a:  br.s       IL_015a
  IL_010c:  ldarg.0
  IL_010d:  ldstr      ""string 6""
  IL_0112:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0117:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_011c:  brtrue.s   IL_0154
  IL_011e:  br.s       IL_015a
  IL_0120:  ldarg.0
  IL_0121:  ldstr      ""string 7""
  IL_0126:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_012b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0130:  brtrue.s   IL_0156
  IL_0132:  br.s       IL_015a
  IL_0134:  ldarg.0
  IL_0135:  ldstr      ""string 8""
  IL_013a:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_013f:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0144:  brtrue.s   IL_0158
  IL_0146:  br.s       IL_015a
  IL_0148:  ldc.i4.0
  IL_0149:  ret
  IL_014a:  ldc.i4.1
  IL_014b:  ret
  IL_014c:  ldc.i4.2
  IL_014d:  ret
  IL_014e:  ldc.i4.3
  IL_014f:  ret
  IL_0150:  ldc.i4.4
  IL_0151:  ret
  IL_0152:  ldc.i4.5
  IL_0153:  ret
  IL_0154:  ldc.i4.6
  IL_0155:  ret
  IL_0156:  ldc.i4.7
  IL_0157:  ret
  IL_0158:  ldc.i4.8
  IL_0159:  ret
  IL_015a:  ldc.i4.s   9
  IL_015c:  ret
}");

            compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3
4
5
6
7
8
9")
                .VerifyIL("C.GetResult", @"
{
  // Code size      270 (0x10e)
  .maxstack  2
  .locals init (int V_0,
                char V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<char>.Length.get""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  brfalse    IL_00f9
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.8
  IL_0010:  bne.un     IL_010b
  IL_0015:  ldarga.s   V_0
  IL_0017:  ldc.i4.7
  IL_0018:  call       ""ref char System.Span<char>.this[int].get""
  IL_001d:  ldind.u2
  IL_001e:  stloc.1
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.s   49
  IL_0022:  sub
  IL_0023:  switch    (
        IL_004d,
        IL_0067,
        IL_0081,
        IL_0095,
        IL_00a9,
        IL_00bd,
        IL_00d1,
        IL_00e5)
  IL_0048:  br         IL_010b
  IL_004d:  ldarg.0
  IL_004e:  ldstr      ""string 1""
  IL_0053:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0058:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_005d:  brtrue     IL_00fb
  IL_0062:  br         IL_010b
  IL_0067:  ldarg.0
  IL_0068:  ldstr      ""string 2""
  IL_006d:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0072:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0077:  brtrue     IL_00fd
  IL_007c:  br         IL_010b
  IL_0081:  ldarg.0
  IL_0082:  ldstr      ""string 3""
  IL_0087:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_008c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0091:  brtrue.s   IL_00ff
  IL_0093:  br.s       IL_010b
  IL_0095:  ldarg.0
  IL_0096:  ldstr      ""string 4""
  IL_009b:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00a0:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00a5:  brtrue.s   IL_0101
  IL_00a7:  br.s       IL_010b
  IL_00a9:  ldarg.0
  IL_00aa:  ldstr      ""string 5""
  IL_00af:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00b4:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00b9:  brtrue.s   IL_0103
  IL_00bb:  br.s       IL_010b
  IL_00bd:  ldarg.0
  IL_00be:  ldstr      ""string 6""
  IL_00c3:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00c8:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00cd:  brtrue.s   IL_0105
  IL_00cf:  br.s       IL_010b
  IL_00d1:  ldarg.0
  IL_00d2:  ldstr      ""string 7""
  IL_00d7:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00dc:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00e1:  brtrue.s   IL_0107
  IL_00e3:  br.s       IL_010b
  IL_00e5:  ldarg.0
  IL_00e6:  ldstr      ""string 8""
  IL_00eb:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_00f0:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_00f5:  brtrue.s   IL_0109
  IL_00f7:  br.s       IL_010b
  IL_00f9:  ldc.i4.0
  IL_00fa:  ret
  IL_00fb:  ldc.i4.1
  IL_00fc:  ret
  IL_00fd:  ldc.i4.2
  IL_00fe:  ret
  IL_00ff:  ldc.i4.3
  IL_0100:  ret
  IL_0101:  ldc.i4.4
  IL_0102:  ret
  IL_0103:  ldc.i4.5
  IL_0104:  ret
  IL_0105:  ldc.i4.6
  IL_0106:  ret
  IL_0107:  ldc.i4.7
  IL_0108:  ret
  IL_0109:  ldc.i4.8
  IL_010a:  ret
  IL_010b:  ldc.i4.s   9
  IL_010d:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchSpanCharOnConstantStringAndOtherPatterns()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test(""string 1"".ToArray());
        Test(""string 2"".ToArray());
        Test(""string 3"".ToArray());
    }
    static void Test(Span<char> chars) 
    {
        var number = chars switch {
            { Length: 0 } => 0,
            ""string 1"" and [..,'1'] => 1,
            { Length: 8 } and ""string 2"" => 2,
            _ => 3,
        };
        Console.WriteLine(number);
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"0
1
2
3")
                .VerifyIL("C.Test", @"
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<char>.Length.get""
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  brfalse.s  IL_0046
  IL_000b:  ldarg.0
  IL_000c:  ldstr      ""string 1""
  IL_0011:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0016:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_001b:  brfalse.s  IL_002e
  IL_001d:  ldarga.s   V_0
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  sub
  IL_0022:  call       ""ref char System.Span<char>.this[int].get""
  IL_0027:  ldind.u2
  IL_0028:  ldc.i4.s   49
  IL_002a:  beq.s      IL_004a
  IL_002c:  br.s       IL_0052
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.8
  IL_0030:  bne.un.s   IL_0052
  IL_0032:  ldarg.0
  IL_0033:  ldstr      ""string 2""
  IL_0038:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_003d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0042:  brtrue.s   IL_004e
  IL_0044:  br.s       IL_0052
  IL_0046:  ldc.i4.0
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_0054
  IL_004a:  ldc.i4.1
  IL_004b:  stloc.0
  IL_004c:  br.s       IL_0054
  IL_004e:  ldc.i4.2
  IL_004f:  stloc.0
  IL_0050:  br.s       IL_0054
  IL_0052:  ldc.i4.3
  IL_0053:  stloc.0
  IL_0054:  ldloc.0
  IL_0055:  call       ""void System.Console.WriteLine(int)""
  IL_005a:  ret
}");
        }

        [Fact]
        public void PatternMatchSpanCharOnConstantStringInOrAndAndNot()
        {
            var source =
    @"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test(""string 1"".ToArray());
        Test(""string 2"".ToArray());
        Test(""string 3"".ToArray());
    }
    static void Test(Span<char> chars)
    {
        Console.WriteLine(""or: "" + (chars is ""string 1"" or ""string 2""));
        Console.WriteLine(""and: "" + (chars is ""string 1"" and { Length: 7 }));
        Console.WriteLine(""not: "" + (chars is not ""string 1""));
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"or: True
and: False
not: False
or: True
and: False
not: True
or: False
and: False
not: True")
                .VerifyIL("C.Test", """
{
  // Code size      167 (0xa7)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldstr      "string 1"
  IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_000c:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0011:  brtrue.s   IL_0027
  IL_0013:  ldarg.0
  IL_0014:  ldstr      "string 2"
  IL_0019:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_001e:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0023:  brtrue.s   IL_0027
  IL_0025:  br.s       IL_002b
  IL_0027:  ldc.i4.1
  IL_0028:  stloc.0
  IL_0029:  br.s       IL_002d
  IL_002b:  ldc.i4.0
  IL_002c:  stloc.0
  IL_002d:  ldstr      "or: "
  IL_0032:  ldloca.s   V_0
  IL_0034:  call       "string bool.ToString()"
  IL_0039:  call       "string string.Concat(string, string)"
  IL_003e:  call       "void System.Console.WriteLine(string)"
  IL_0043:  nop
  IL_0044:  ldstr      "and: "
  IL_0049:  ldarg.0
  IL_004a:  ldstr      "string 1"
  IL_004f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_0054:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0059:  brfalse.s  IL_0067
  IL_005b:  ldarga.s   V_0
  IL_005d:  call       "int System.Span<char>.Length.get"
  IL_0062:  ldc.i4.7
  IL_0063:  ceq
  IL_0065:  br.s       IL_0068
  IL_0067:  ldc.i4.0
  IL_0068:  stloc.0
  IL_0069:  ldloca.s   V_0
  IL_006b:  call       "string bool.ToString()"
  IL_0070:  call       "string string.Concat(string, string)"
  IL_0075:  call       "void System.Console.WriteLine(string)"
  IL_007a:  nop
  IL_007b:  ldstr      "not: "
  IL_0080:  ldarg.0
  IL_0081:  ldstr      "string 1"
  IL_0086:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
  IL_008b:  call       "bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)"
  IL_0090:  ldc.i4.0
  IL_0091:  ceq
  IL_0093:  stloc.0
  IL_0094:  ldloca.s   V_0
  IL_0096:  call       "string bool.ToString()"
  IL_009b:  call       "string string.Concat(string, string)"
  IL_00a0:  call       "void System.Console.WriteLine(string)"
  IL_00a5:  nop
  IL_00a6:  ret
}
""");
        }

        [Fact]
        public void RecursivePatternMatchSpanCharOnConstantString()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test(new S { Span = ""string 1"".ToArray(), Prop = true });
        Test(new S { Span = ""string 1"".ToArray(), Prop = false });
        Test(new S { Span = ""string 2"".ToArray(), Prop = true });
        Test(new S { Span = ""string 2"".ToArray(), Prop = false });
    }
    static void Test(S s) => Console.WriteLine(s is { Prop: true, Span: ""string 1"" and { Length: 8 } });
}

ref struct S
{
    public Span<char> Span { get; set; }
    public bool Prop { get; set; }
}";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics();
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: @"True
False
False
False")
                .VerifyIL("C.Test", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Span<char> V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool S.Prop.get""
  IL_0007:  brfalse.s  IL_002f
  IL_0009:  ldarga.s   V_0
  IL_000b:  call       ""readonly System.Span<char> S.Span.get""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldstr      ""string 1""
  IL_0017:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0021:  brfalse.s  IL_002f
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""int System.Span<char>.Length.get""
  IL_002a:  ldc.i4.8
  IL_002b:  ceq
  IL_002d:  br.s       IL_0030
  IL_002f:  ldc.i4.0
  IL_0030:  call       ""void System.Console.WriteLine(bool)""
  IL_0035:  nop
  IL_0036:  ret
}");
        }

        [Fact]
        public void PatternMatchSpanCharOnConstantStringMissingMemoryExtensions()
        {
            var source =
@"
using System;
class C
{
    static bool M(Span<char> chars) => chars is """";
}
";
            CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (5,49): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //     static bool M(Span<char> chars) => chars is "";
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(5, 49),
                    // (5,49): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //     static bool M(Span<char> chars) => chars is "";
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(5, 49));
        }

        [Fact]
        public void SwitchSpanCharOnConstantStringMissingMemoryExtensions()
        {
            var source =
@"
using System;
class C
{
    static int M(Span<char> chars) 
    {
        return chars switch {
            """" => 0,
            ""string 1"" => 1,
            ""string 2"" => 2,
            _ => 3,
        };
    }
}
";
            CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "" => 0,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(8, 13),
                    // (8,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "" => 0,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(8, 13),
                    // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "string 1" => 1,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 1""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(9, 13),
                    // (9,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "string 1" => 1,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 1""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(9, 13),
                    // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.SequenceEqual'
                    //             "string 2" => 2,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 2""").WithArguments("System.MemoryExtensions", "SequenceEqual").WithLocation(10, 13),
                    // (10,13): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                    //             "string 2" => 2,
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""string 2""").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(10, 13));
        }

        [Fact]
        public void PatternOrSwitchSpanChar_MissingLengthAndIndexer()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        public Span(T[] array) { }
    }
    public ref struct ReadOnlySpan<T>
    {
        public ReadOnlySpan(T[] array) { }
    }
    public static class MemoryExtensions
    {
        public static ReadOnlySpan<char> AsSpan(string s) => default;
        public static bool SequenceEqual<T>(Span<T> a, ReadOnlySpan<T> b) => false;
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var s = new Span<char>(new char[0]);
        _ = s is ""str"";
        _ = s is { Length: 0 } and """";
        _ = s switch { ""str"" => 1, _ => 0 };
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (7,18): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
                //         _ = s is "str";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""str""").WithArguments("System.Span`1", "get_Length").WithLocation(7, 18),
                // (8,20): error CS0117: 'Span<char>' does not contain a definition for 'Length'
                //         _ = s is { Length: 0 } and "";
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Length").WithArguments("System.Span<char>", "Length").WithLocation(8, 20),
                // (8,36): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
                //         _ = s is { Length: 0 } and "";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""""").WithArguments("System.Span`1", "get_Length").WithLocation(8, 36),
                // (9,24): error CS0656: Missing compiler required member 'System.Span`1.get_Length'
                //         _ = s switch { "str" => 1, _ => 0 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""str""").WithArguments("System.Span`1", "get_Length").WithLocation(9, 24));
        }

        [Fact]
        public void PatternMatchSpanCharOnConstantStringCSharp10()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static bool M(Span<char> chars) => chars is """";
    static void Main()
    {
        Console.WriteLine(M(new Span<char>(null)));
        Console.WriteLine(M("""".ToArray()));
        Console.WriteLine(M(""str"".ToArray()));
    }
}
";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,49): error CS8936: Feature 'pattern matching ReadOnly/Span<char> on constant string' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static bool M(Span<char> chars) => chars is "";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""").WithArguments("pattern matching ReadOnly/Span<char> on constant string", "11.0").WithLocation(5, 49));

            comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
False");
            verifier.VerifyIL("C.M",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldstr      """"
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void SwitchSpanCharOnConstantStringCSharp10()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static bool M(Span<char> chars) => chars switch { """" => true, _ => false };
    static void Main()
    {
        Console.WriteLine(M(new Span<char>(null)));
        Console.WriteLine(M("""".ToArray()));
        Console.WriteLine(M(""str"".ToArray()));
    }
}
";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,55): error CS8936: Feature 'pattern matching ReadOnly/Span<char> on constant string' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     static bool M(Span<char> chars) => chars switch { "" => true, _ => false };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, @"""""").WithArguments("pattern matching ReadOnly/Span<char> on constant string", "11.0").WithLocation(5, 55));

            comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
False");
            verifier.VerifyIL("C.M",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      """"
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0010:  brfalse.s  IL_0016
  IL_0012:  ldc.i4.1
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0018
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ret
}");
        }

        [Fact]
        public void PatternMatchSpanCharOnNull_01()
        {
            var source =
@"
using System;
class C
{
    static bool M1(Span<char> chars) => chars is null;
    static bool M2(Span<char> chars) => chars is default;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,50): error CS9133: A constant value of type 'Span<char>' is expected
                //     static bool M1(Span<char> chars) => chars is null;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "null").WithArguments("System.Span<char>").WithLocation(5, 50),
                // (6,50): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //     static bool M2(Span<char> chars) => chars is default;
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(6, 50));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void PatternMatchSpanCharOnNull_02()
        {
            var source =
@"using System;
class C
{
    static bool M(Span<char> chars) => chars is (object)null;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,49): error CS0266: Cannot implicitly convert type 'object' to 'System.Span<char>'. An explicit conversion exists (are you missing a cast?)
                //     static bool M(Span<char> chars) => chars is (object)null;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "System.Span<char>").WithLocation(4, 49));
        }

        [Fact]
        public void PatternMatchSpanCharOnNull_03()
        {
            var source =
@"using System;
class C
{
    static bool M1(Span<char> chars) => chars is (string)null;
    static bool M2(Span<char> chars) => chars is default(string);
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,50): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //     static bool M1(Span<char> chars) => chars is (string)null;
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "(string)null").WithArguments("System.Span<char>").WithLocation(4, 50),
                // (5,50): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //     static bool M2(Span<char> chars) => chars is default(string);
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "default(string)").WithArguments("System.Span<char>").WithLocation(5, 50));
        }

        [Fact]
        public void PatternMatchSpanCharOnNull_04()
        {
            var source =
@"using System;
class C
{
    const string NullString = null;
    static bool M(Span<char> chars) => chars is NullString;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,49): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //     static bool M(Span<char> chars) => chars is NullString;
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "NullString").WithArguments("System.Span<char>").WithLocation(5, 49));
        }

        [Fact]
        public void SwitchSpanCharOnNull_01()
        {
            var source =
@"
using System;
class C
{
    static bool M(Span<char> chars) => chars switch { null => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
            .VerifyDiagnostics(
                // (5,55): error CS9133: A constant value of type 'Span<char>' is expected
                //     static bool M(Span<char> chars) => chars switch { null => true, _ => false };
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "null").WithArguments("System.Span<char>").WithLocation(5, 55)
            );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchSpanCharOnNull_02()
        {
            var source =
@"using System;
class C
{
    static bool M(Span<char> chars) => chars switch { (object)null => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,55): error CS0266: Cannot implicitly convert type 'object' to 'System.Span<char>'. An explicit conversion exists (are you missing a cast?)
                //     static bool M(Span<char> chars) => chars switch { (object)null => true, _ => false };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "System.Span<char>").WithLocation(4, 55));
        }

        [Fact]
        public void SwitchSpanCharOnNull_03()
        {
            var source =
@"using System;
class C
{
    static bool M(Span<char> chars) => chars switch { (string)null => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,55): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //     static bool M(Span<char> chars) => chars switch { (string)null => true, _ => false };
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "(string)null").WithArguments("System.Span<char>").WithLocation(4, 55));
        }

        [Fact]
        public void SwitchSpanCharOnNull_04()
        {
            var source =
@"using System;
class C
{
    const string NullString = null;
    static bool M(Span<char> chars) => chars switch { NullString => true, _ => false };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,55): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //     static bool M(Span<char> chars) => chars switch { NullString => true, _ => false };
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "NullString").WithArguments("System.Span<char>").WithLocation(5, 55));
        }

        [Fact]
        public void MatchSpanCharOnImpossiblePatterns()
        {
            var source =
@"
using System;
class C
{
    static void M(Span<char> chars)
    {
        _ = chars is """" and "" "";
        _ = chars is """" and not """";
        _ = chars is """" and ("" "" or not """");
    }
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (7,13): error CS8518: An expression of type 'Span<char>' can never match the provided pattern.
                    //         _ = chars is "" and " ";
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and "" """).WithArguments("System.Span<char>").WithLocation(7, 13),
                    // (8,13): error CS8518: An expression of type 'Span<char>' can never match the provided pattern.
                    //         _ = chars is "" and not "";
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and not """"").WithArguments("System.Span<char>").WithLocation(8, 13),
                    // (9,13): error CS8518: An expression of type 'Span<char>' can never match the provided pattern.
                    //         _ = chars is "" and (" " or not "");
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"chars is """" and ("" "" or not """")").WithArguments("System.Span<char>").WithLocation(9, 13));
        }

        [Fact]
        public void PatternMatchSpanCharOnPossiblePatterns()
        {
            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        Test("""".ToArray());
        Test("" "".ToArray());
        Test(""  "".ToArray());
    }
    static void Test(Span<char> chars)
    {
        Console.WriteLine(""1."" + (chars is """" and not "" ""));
        Console.WriteLine(""2."" + (chars is """" and ("" "" or """")));
        Console.WriteLine(""3."" + (chars is """" or """"));
        Console.WriteLine(""4."" + (chars is """" or not """"));
    }
}
";
            var compilation = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview)
                .VerifyEmitDiagnostics(
                    // (15,55): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("1." + (chars is "" and not " "));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" """).WithLocation(15, 55),
                    // (16,52): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("2." + (chars is "" and (" " or "")));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" """).WithLocation(16, 52),
                    // (16,52): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("2." + (chars is "" and (" " or "")));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @""" "" or """"").WithLocation(16, 52),
                    // (17,50): hidden CS9335: The pattern is redundant.
                    //         Console.WriteLine("3." + (chars is "" or ""));
                    Diagnostic(ErrorCode.HDN_RedundantPattern, @"""""").WithLocation(17, 50),
                    // (18,35): warning CS8794: An expression of type 'Span<char>' always matches the provided pattern.
                    //         Console.WriteLine("4." + (chars is "" or not ""));
                    Diagnostic(ErrorCode.WRN_IsPatternAlways, @"chars is """" or not """"").WithArguments("System.Span<char>").WithLocation(18, 35));
            CompileAndVerify(compilation, expectedOutput: @"1.True
2.True
3.True
4.True
1.False
2.False
3.False
4.True
1.False
2.False
3.False
4.True")
                .VerifyIL("C.Test", @"
{
  // Code size      159 (0x9f)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  ldstr      ""1.""
  IL_0005:  ldarg.0
  IL_0006:  ldstr      """"
  IL_000b:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0010:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0015:  stloc.0
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       ""string bool.ToString()""
  IL_001d:  call       ""string string.Concat(string, string)""
  IL_0022:  call       ""void System.Console.WriteLine(string)""
  IL_0027:  ldstr      ""2.""
  IL_002c:  ldarg.0
  IL_002d:  ldstr      """"
  IL_0032:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0037:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_003c:  stloc.0
  IL_003d:  ldloca.s   V_0
  IL_003f:  call       ""string bool.ToString()""
  IL_0044:  call       ""string string.Concat(string, string)""
  IL_0049:  call       ""void System.Console.WriteLine(string)""
  IL_004e:  ldstr      ""3.""
  IL_0053:  ldarg.0
  IL_0054:  ldstr      """"
  IL_0059:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_005e:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0063:  stloc.0
  IL_0064:  ldloca.s   V_0
  IL_0066:  call       ""string bool.ToString()""
  IL_006b:  call       ""string string.Concat(string, string)""
  IL_0070:  call       ""void System.Console.WriteLine(string)""
  IL_0075:  ldarg.0
  IL_0076:  ldstr      """"
  IL_007b:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0080:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0085:  pop
  IL_0086:  ldc.i4.1
  IL_0087:  stloc.0
  IL_0088:  ldstr      ""4.""
  IL_008d:  ldloca.s   V_0
  IL_008f:  call       ""string bool.ToString()""
  IL_0094:  call       ""string string.Concat(string, string)""
  IL_0099:  call       ""void System.Console.WriteLine(string)""
  IL_009e:  ret
}");
        }

        [Fact]
        public void SwitchSpanCharOnDuplicateString()
        {
            var source =
@"
using System;
class C
{
    static bool M(Span<char> chars) => chars switch {
        """" => true,
        """" => false,
        _ => false,
    };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (7,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                    //         "" => false,
                    Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""""").WithLocation(7, 9));
        }

        [Fact]
        public void PatternMatchSpanOfT_01()
        {
            var source =
@"using System;
class Program
{
    static bool F1<T>(ReadOnlySpan<T> span) => span is """";
    static bool F2<T>(Span<T> span) => span is """";
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (4,56): error CS8121: An expression of type 'ReadOnlySpan<T>' cannot be handled by a pattern of type 'string'.
                //     static bool F1<T>(ReadOnlySpan<T> span) => span is "";
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""""").WithArguments("System.ReadOnlySpan<T>", "string").WithLocation(4, 56),
                // (5,48): error CS8121: An expression of type 'Span<T>' cannot be handled by a pattern of type 'string'.
                //     static bool F2<T>(Span<T> span) => span is "";
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""""").WithArguments("System.Span<T>", "string").WithLocation(5, 48));
        }

        [Fact]
        public void PatternMatchSpanOfT_02()
        {
            var source =
@"using System;
using System.Linq;
class Program
{
    static bool F1<T>(ReadOnlySpan<T> span) => span is ReadOnlySpan<char> _;
    static bool F2<T>(Span<T> span) => span is Span<char> _;
    static void F<T>(Span<T> span)
    {
        Console.WriteLine((F1((ReadOnlySpan<T>)span), F2(span)));
    }
    static void Main()
    {
        F<char>("""".ToArray());
        F(new Span<char>(new char[] { '1', '2', '3' }));
        F(new Span<int>(new int[] { '1', '2', '3' }));
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (5,56): error CS8121: An expression of type 'ReadOnlySpan<T>' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F1<T>(ReadOnlySpan<T> span) => span is ReadOnlySpan<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("System.ReadOnlySpan<T>", "System.ReadOnlySpan<char>").WithLocation(5, 56),
                // (6,48): error CS8121: An expression of type 'Span<T>' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F2<T>(Span<T> span) => span is Span<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("System.Span<T>", "System.Span<char>").WithLocation(6, 48));
        }

        [Fact]
        public void PatternMatchSpanOfT_03()
        {
            var source =
@"using System;
class Program
{
    static bool F1<T>(ReadOnlySpan<T> span) => span is ReadOnlySpan<char> and ""ABC"";
    static bool F2<T>(Span<T> span) => span is Span<char> and ""123"";
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (4,56): error CS8121: An expression of type 'ReadOnlySpan<T>' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F1<T>(ReadOnlySpan<T> span) => span is ReadOnlySpan<char> and "ABC";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("System.ReadOnlySpan<T>", "System.ReadOnlySpan<char>").WithLocation(4, 56),
                // (5,48): error CS8121: An expression of type 'Span<T>' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F2<T>(Span<T> span) => span is Span<char> and "123";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("System.Span<T>", "System.Span<char>").WithLocation(5, 48));
        }

        [Fact]
        public void PatternMatchSpanChar_BaseType_01()
        {
            var source =
@"using System;
class Program
{
    static bool F1<T>(object o) => o is ReadOnlySpan<char> _;
    static bool F2<T>(object o) => o is Span<char> _;
    static bool F3<T>(ValueType v) => v is ReadOnlySpan<char> _;
    static bool F4<T>(ValueType v) => v is Span<char> _;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (4,41): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F1<T>(object o) => o is ReadOnlySpan<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("object", "System.ReadOnlySpan<char>").WithLocation(4, 41),
                // (5,41): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F2<T>(object o) => o is Span<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("object", "System.Span<char>").WithLocation(5, 41),
                // (6,44): error CS8121: An expression of type 'ValueType' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F3<T>(ValueType v) => v is ReadOnlySpan<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("System.ValueType", "System.ReadOnlySpan<char>").WithLocation(6, 44),
                // (7,44): error CS8121: An expression of type 'ValueType' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F4<T>(ValueType v) => v is Span<char> _;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("System.ValueType", "System.Span<char>").WithLocation(7, 44));
        }

        [Fact]
        public void PatternMatchSpanChar_BaseType_02()
        {
            var source =
@"using System;
class Program
{
    static bool F1<T>(object o) => o is ReadOnlySpan<char> and ""ABC"";
    static bool F2<T>(object o) => o is Span<char> and ""123"";
    static bool F3<T>(ValueType v) => v is ReadOnlySpan<char> and ""ABC"";
    static bool F4<T>(ValueType v) => v is Span<char> and ""123"";
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (4,41): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F1<T>(object o) => o is ReadOnlySpan<char> and "ABC";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("object", "System.ReadOnlySpan<char>").WithLocation(4, 41),
                // (5,41): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F2<T>(object o) => o is Span<char> and "123";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("object", "System.Span<char>").WithLocation(5, 41),
                // (6,44): error CS8121: An expression of type 'ValueType' cannot be handled by a pattern of type 'ReadOnlySpan<char>'.
                //     static bool F3<T>(ValueType v) => v is ReadOnlySpan<char> and "ABC";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "ReadOnlySpan<char>").WithArguments("System.ValueType", "System.ReadOnlySpan<char>").WithLocation(6, 44),
                // (7,44): error CS8121: An expression of type 'ValueType' cannot be handled by a pattern of type 'Span<char>'.
                //     static bool F4<T>(ValueType v) => v is Span<char> and "123";
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<char>").WithArguments("System.ValueType", "System.Span<char>").WithLocation(7, 44));
        }

        [Fact]
        public void PatternMatchSpanChar_InterpolatedString_01()
        {
            var source =
@"using System;
class Program
{
    const int n = 123;
    static bool F1(ReadOnlySpan<char> span) => span is $""{123}"";
    static bool F2(Span<char> span) => span is $""{n}"";
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (5,56): error CS9133: A constant value of type 'ReadOnlySpan<char>' is expected
                //     static bool F1(ReadOnlySpan<char> span) => span is $"{123}";
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"$""{123}""").WithArguments("System.ReadOnlySpan<char>").WithLocation(5, 56),
                // (6,48): error CS9133: A constant value of type 'Span<char>' is expected
                //     static bool F2(Span<char> span) => span is $"{n}";
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"$""{n}""").WithArguments("System.Span<char>").WithLocation(6, 48));
        }

        [Fact]
        public void PatternMatchSpanChar_InterpolatedString_02()
        {
            var source =
@"using System;
using System.Linq;
class Program
{
    const string s = ""123"";
    static bool F1(ReadOnlySpan<char> span) => span is $"""";
    static bool F2(Span<char> span) => span is $""{s}"";
    static void F(Span<char> span)
    {
        Console.WriteLine((F1((ReadOnlySpan<char>)span), F2(span)));
    }
    static void Main()
    {
        F("""".ToArray());
        F(""123"".ToArray());
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"(True, False)
(False, True)
");
        }

        [Fact]
        public void PatternMatchSpanChar_Conditional_01()
        {
            var source =
@"using System;
class Program
{
    static bool F1(ReadOnlySpan<char> span, bool b) => span is (b ? """" : ""ABC"");
    static bool F2(Span<char> span, bool b) => span is (b ? """" : ""123"");
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (4,65): error CS9133: A constant value of type 'ReadOnlySpan<char>' is expected
                //     static bool F1(ReadOnlySpan<char> span, bool b) => span is (b ? "" : "ABC");
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"b ? """" : ""ABC""").WithArguments("System.ReadOnlySpan<char>").WithLocation(4, 65),
                // (5,57): error CS9133: A constant value of type 'Span<char>' is expected
                //     static bool F2(Span<char> span, bool b) => span is (b ? "" : "123");
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"b ? """" : ""123""").WithArguments("System.Span<char>").WithLocation(5, 57));
        }

        [Fact]
        public void PatternMatchSpanChar_Conditional_02()
        {
            var source =
@"using System;
using System.Linq;
class Program
{
    static bool F1(ReadOnlySpan<char> span) => span is (true ? """" : ""ABC"");
    static bool F2(Span<char> span) => span is (false ? """" : ""123"");
    static void F(Span<char> span)
    {
        Console.WriteLine((F1((ReadOnlySpan<char>)span), F2(span)));
    }
    static void Main()
    {
        F("""".ToArray());
        F(""123"".ToArray());
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"(True, False)
(False, True)
");
        }

        [Fact]
        public void PatternMatchSpanChar_SwitchExpression()
        {
            var source =
@"using System;
class Program
{
    static bool F1(ReadOnlySpan<char> span, bool b) => span is b switch { true => """", false => ""ABC"" };
    static bool F2(Span<char> span, bool b) => span is b switch { false => """", true => ""123"" };
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (4,64): error CS9133: A constant value of type 'ReadOnlySpan<char>' is expected
                //     static bool F1(ReadOnlySpan<char> span, bool b) => span is b switch { true => "", false => "ABC" };
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"b switch { true => """", false => ""ABC"" }").WithArguments("System.ReadOnlySpan<char>").WithLocation(4, 64),
                // (5,56): error CS9133: A constant value of type 'Span<char>' is expected
                //     static bool F2(Span<char> span, bool b) => span is b switch { false => "", true => "123" };
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"b switch { false => """", true => ""123"" }").WithArguments("System.Span<char>").WithLocation(5, 56));
        }

        [Fact]
        public void PatternMatchSpanChar_ExpressionTree_01()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) is ""123"";
        Expression<Func<bool>> e2 = () => new Span<char>(null) is ""ABC"";
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (7,43): error CS8122: An expression tree may not contain an 'is' pattern-matching operator.
                //         Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) is "123";
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIsMatch, @"new ReadOnlySpan<char>(null) is ""123""").WithLocation(7, 43),
                // (7,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) is "123";
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "new ReadOnlySpan<char>(null)").WithArguments("ReadOnlySpan").WithLocation(7, 43),
                // (8,43): error CS8122: An expression tree may not contain an 'is' pattern-matching operator.
                //         Expression<Func<bool>> e2 = () => new Span<char>(null) is "ABC";
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIsMatch, @"new Span<char>(null) is ""ABC""").WithLocation(8, 43),
                // (8,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
                //         Expression<Func<bool>> e2 = () => new Span<char>(null) is "ABC";
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "new Span<char>(null)").WithArguments("Span").WithLocation(8, 43));
        }

        [Fact]
        public void PatternMatchSpanChar_ExpressionTree_02()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) switch { ""123"" => true, _ => false };
        Expression<Func<bool>> e2 = () => new Span<char>(null) switch { ""ABC"" => true, _ => false };
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (7,43): error CS8514: An expression tree may not contain a switch expression.
                //         Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) switch { "123" => true, _ => false };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsSwitchExpression, @"new ReadOnlySpan<char>(null) switch { ""123"" => true, _ => false }").WithLocation(7, 43),
                // (7,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
                //         Expression<Func<bool>> e1 = () => new ReadOnlySpan<char>(null) switch { "123" => true, _ => false };
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "new ReadOnlySpan<char>(null)").WithArguments("ReadOnlySpan").WithLocation(7, 43),
                // (8,43): error CS8514: An expression tree may not contain a switch expression.
                //         Expression<Func<bool>> e2 = () => new Span<char>(null) switch { "ABC" => true, _ => false };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsSwitchExpression, @"new Span<char>(null) switch { ""ABC"" => true, _ => false }").WithLocation(8, 43),
                // (8,43): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
                //         Expression<Func<bool>> e2 = () => new Span<char>(null) switch { "ABC" => true, _ => false };
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "new Span<char>(null)").WithArguments("Span").WithLocation(8, 43));
        }

        /// <summary>
        /// Ensure that the synthesized switch hash method can handle a null
        /// value if null is supported as a constant pattern in the future.
        /// </summary>
        [Fact]
        public void PatternMatchSpanChar_SwitchHashWithNull()
        {
            var source =
@"using System;
class Program
{
    static int F1(ReadOnlySpan<char> span)
    {
        return span switch
        {
            (string)null => 0,
            ""1"" => 1,
            ""2"" => 2,
            ""3"" => 3,
            ""4"" => 4,
            ""5"" => 5,
            ""6"" => 6,
            _ => 7,
        };
    }
    static int F2(Span<char> span)
    {
        return span switch
        {
            ""1"" => 1,
            ""2"" => 2,
            ""3"" => 3,
            ""4"" => 4,
            ""5"" => 5,
            ""6"" => 6,
            default(string) => 7,
            _ => 0,
        };
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (8,13): error CS9013: A string 'null' constant is not supported as a pattern for 'ReadOnlySpan<char>'. Use an empty string instead.
                //             (string)null => 0,
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "(string)null").WithArguments("System.ReadOnlySpan<char>").WithLocation(8, 13),
                // (28,13): error CS9013: A string 'null' constant is not supported as a pattern for 'Span<char>'. Use an empty string instead.
                //             default(string) => 7,
                Diagnostic(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, "default(string)").WithArguments("System.Span<char>").WithLocation(28, 13));
        }

        /// <summary>
        /// DecisionDagRewriter.EnsureStringHashFunction() does not generate
        /// a hash function if the span indexer is missing.
        /// </summary>
        [Fact]
        public void PatternMatchSpanChar_MissingIndexer()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public int Length => _array.Length;
        internal T Get(int index) => _array[index];
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
        public int Length => _array.Length;
        internal T Get(int index) => _array[index];
    }
    public static class MemoryExtensions
    {
        public static ReadOnlySpan<char> AsSpan(string s)
        {
            var array = new char[s.Length];
            for (int i = 0; i < s.Length; i++) array[i] = s[i];
            return new ReadOnlySpan<char>(array);
        }
        public static bool SequenceEqual<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!object.Equals(a.Get(i), b.Get(i))) return false;
            return true;
        }
        public static bool SequenceEqual<T>(Span<T> a, ReadOnlySpan<T> b)
        {
            return SequenceEqual((ReadOnlySpan<T>)a, b);
        }
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        var r = new ReadOnlySpan<char>(new [] { '3' });
        var s = new Span<char>(new [] { '6' });
        Console.WriteLine((F1(r), F2(s)));
    }
    static int F1(ReadOnlySpan<char> s)
    {
        return s switch
        {
            ""1"" => 1,
            ""2"" => 2,
            ""3"" => 3,
            ""4"" => 4,
            ""5"" => 5,
            ""6"" => 6,
            ""7"" => 7,
            _ => 0
        };
    }
    static int F2(Span<char> s)
    {
        return s switch
        {
            ""1"" => 1,
            ""2"" => 2,
            ""3"" => 3,
            ""4"" => 4,
            ""5"" => 5,
            ""6"" => 6,
            ""7"" => 7,
            _ => 0
        };
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "(3, 6)");
            verifier.VerifyIL("Program.F1",
@"{
  // Code size      160 (0xa0)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""1""
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0010:  brtrue.s   IL_0080
  IL_0012:  ldarg.0
  IL_0013:  ldstr      ""2""
  IL_0018:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0022:  brtrue.s   IL_0084
  IL_0024:  ldarg.0
  IL_0025:  ldstr      ""3""
  IL_002a:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_002f:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0034:  brtrue.s   IL_0088
  IL_0036:  ldarg.0
  IL_0037:  ldstr      ""4""
  IL_003c:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0041:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0046:  brtrue.s   IL_008c
  IL_0048:  ldarg.0
  IL_0049:  ldstr      ""5""
  IL_004e:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0053:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0058:  brtrue.s   IL_0090
  IL_005a:  ldarg.0
  IL_005b:  ldstr      ""6""
  IL_0060:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0065:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_006a:  brtrue.s   IL_0094
  IL_006c:  ldarg.0
  IL_006d:  ldstr      ""7""
  IL_0072:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0077:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_007c:  brtrue.s   IL_0098
  IL_007e:  br.s       IL_009c
  IL_0080:  ldc.i4.1
  IL_0081:  stloc.0
  IL_0082:  br.s       IL_009e
  IL_0084:  ldc.i4.2
  IL_0085:  stloc.0
  IL_0086:  br.s       IL_009e
  IL_0088:  ldc.i4.3
  IL_0089:  stloc.0
  IL_008a:  br.s       IL_009e
  IL_008c:  ldc.i4.4
  IL_008d:  stloc.0
  IL_008e:  br.s       IL_009e
  IL_0090:  ldc.i4.5
  IL_0091:  stloc.0
  IL_0092:  br.s       IL_009e
  IL_0094:  ldc.i4.6
  IL_0095:  stloc.0
  IL_0096:  br.s       IL_009e
  IL_0098:  ldc.i4.7
  IL_0099:  stloc.0
  IL_009a:  br.s       IL_009e
  IL_009c:  ldc.i4.0
  IL_009d:  stloc.0
  IL_009e:  ldloc.0
  IL_009f:  ret
}");
            verifier.VerifyIL("Program.F2",
@"{
  // Code size      160 (0xa0)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""1""
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0010:  brtrue.s   IL_0080
  IL_0012:  ldarg.0
  IL_0013:  ldstr      ""2""
  IL_0018:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_001d:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0022:  brtrue.s   IL_0084
  IL_0024:  ldarg.0
  IL_0025:  ldstr      ""3""
  IL_002a:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_002f:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0034:  brtrue.s   IL_0088
  IL_0036:  ldarg.0
  IL_0037:  ldstr      ""4""
  IL_003c:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0041:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0046:  brtrue.s   IL_008c
  IL_0048:  ldarg.0
  IL_0049:  ldstr      ""5""
  IL_004e:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0053:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0058:  brtrue.s   IL_0090
  IL_005a:  ldarg.0
  IL_005b:  ldstr      ""6""
  IL_0060:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0065:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_006a:  brtrue.s   IL_0094
  IL_006c:  ldarg.0
  IL_006d:  ldstr      ""7""
  IL_0072:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_0077:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_007c:  brtrue.s   IL_0098
  IL_007e:  br.s       IL_009c
  IL_0080:  ldc.i4.1
  IL_0081:  stloc.0
  IL_0082:  br.s       IL_009e
  IL_0084:  ldc.i4.2
  IL_0085:  stloc.0
  IL_0086:  br.s       IL_009e
  IL_0088:  ldc.i4.3
  IL_0089:  stloc.0
  IL_008a:  br.s       IL_009e
  IL_008c:  ldc.i4.4
  IL_008d:  stloc.0
  IL_008e:  br.s       IL_009e
  IL_0090:  ldc.i4.5
  IL_0091:  stloc.0
  IL_0092:  br.s       IL_009e
  IL_0094:  ldc.i4.6
  IL_0095:  stloc.0
  IL_0096:  br.s       IL_009e
  IL_0098:  ldc.i4.7
  IL_0099:  stloc.0
  IL_009a:  br.s       IL_009e
  IL_009c:  ldc.i4.0
  IL_009d:  stloc.0
  IL_009e:  ldloc.0
  IL_009f:  ret
}");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SwitchSpanCharConstantStringAndListPatterns()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var s1 = new Span<char>(new char[0]);
        var s2 = new Span<char>(new[] { 's', 't', 'r' });
        Console.WriteLine(F1(s1));
        Console.WriteLine(F1(s2));
        Console.WriteLine(F2(s1));
        Console.WriteLine(F2(s2));
    }
    static int F1(ReadOnlySpan<char> span) 
    {
        return span switch
        {
            ""str"" => 1,
            [ 's', 't', 'r' ] => 2,
            _ => 0,
        };
    }
    static int F2(Span<char> span) 
    {
        return span switch
        {
            [ 's', 't', 'r' ] => 2,
            ""str"" => 1,
            _ => 0,
        };
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"0
1
0
2");
            verifier.VerifyIL("Program.F1",
@"{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""str""
  IL_0006:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_000b:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)""
  IL_0010:  brtrue.s   IL_0045
  IL_0012:  ldarga.s   V_0
  IL_0014:  call       ""int System.ReadOnlySpan<char>.Length.get""
  IL_0019:  ldc.i4.3
  IL_001a:  bne.un.s   IL_004d
  IL_001c:  ldarga.s   V_0
  IL_001e:  ldc.i4.0
  IL_001f:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0024:  ldind.u2
  IL_0025:  ldc.i4.s   115
  IL_0027:  bne.un.s   IL_004d
  IL_0029:  ldarga.s   V_0
  IL_002b:  ldc.i4.1
  IL_002c:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_0031:  ldind.u2
  IL_0032:  ldc.i4.s   116
  IL_0034:  bne.un.s   IL_004d
  IL_0036:  ldarga.s   V_0
  IL_0038:  ldc.i4.2
  IL_0039:  call       ""ref readonly char System.ReadOnlySpan<char>.this[int].get""
  IL_003e:  ldind.u2
  IL_003f:  ldc.i4.s   114
  IL_0041:  beq.s      IL_0049
  IL_0043:  br.s       IL_004d
  IL_0045:  ldc.i4.1
  IL_0046:  stloc.0
  IL_0047:  br.s       IL_004f
  IL_0049:  ldc.i4.2
  IL_004a:  stloc.0
  IL_004b:  br.s       IL_004f
  IL_004d:  ldc.i4.0
  IL_004e:  stloc.0
  IL_004f:  ldloc.0
  IL_0050:  ret
}");
            verifier.VerifyIL("Program.F2",
@"{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""int System.Span<char>.Length.get""
  IL_0007:  ldc.i4.3
  IL_0008:  bne.un.s   IL_0031
  IL_000a:  ldarga.s   V_0
  IL_000c:  ldc.i4.0
  IL_000d:  call       ""ref char System.Span<char>.this[int].get""
  IL_0012:  ldind.u2
  IL_0013:  ldc.i4.s   115
  IL_0015:  bne.un.s   IL_0031
  IL_0017:  ldarga.s   V_0
  IL_0019:  ldc.i4.1
  IL_001a:  call       ""ref char System.Span<char>.this[int].get""
  IL_001f:  ldind.u2
  IL_0020:  ldc.i4.s   116
  IL_0022:  bne.un.s   IL_0031
  IL_0024:  ldarga.s   V_0
  IL_0026:  ldc.i4.2
  IL_0027:  call       ""ref char System.Span<char>.this[int].get""
  IL_002c:  ldind.u2
  IL_002d:  ldc.i4.s   114
  IL_002f:  beq.s      IL_0045
  IL_0031:  ldarg.0
  IL_0032:  ldstr      ""str""
  IL_0037:  call       ""System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)""
  IL_003c:  call       ""bool System.MemoryExtensions.SequenceEqual<char>(System.Span<char>, System.ReadOnlySpan<char>)""
  IL_0041:  brtrue.s   IL_0049
  IL_0043:  br.s       IL_004d
  IL_0045:  ldc.i4.2
  IL_0046:  stloc.0
  IL_0047:  br.s       IL_004f
  IL_0049:  ldc.i4.1
  IL_004a:  stloc.0
  IL_004b:  br.s       IL_004f
  IL_004d:  ldc.i4.0
  IL_004e:  stloc.0
  IL_004f:  ldloc.0
  IL_0050:  ret
}");
        }

        [Fact]
        public void PatternMatchSpanChar_ObsoleteMemoryExtensions()
        {
            var sourceA =
@"namespace System
{
    public ref struct Span<T>
    {
        private readonly T[] _array;
        public Span(T[] array) { _array = array; }
        public int Length => _array.Length;
        public ref T this[int index] => ref _array[index];
        public static implicit operator ReadOnlySpan<T>(Span<T> s) => new ReadOnlySpan<T>(s._array);
    }
    public ref struct ReadOnlySpan<T>
    {
        private readonly T[] _array;
        public ReadOnlySpan(T[] array) { _array = array; }
        public int Length => _array.Length;
        public ref T this[int index] => ref _array[index];
    }
    [Obsolete]
    public static class MemoryExtensions
    {
        [Obsolete]
        public static ReadOnlySpan<char> AsSpan(string s)
        {
            var array = new char[s.Length];
            for (int i = 0; i < s.Length; i++) array[i] = s[i];
            return new ReadOnlySpan<char>(array);
        }
        [Obsolete]
        public static bool SequenceEqual<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!object.Equals(a[i], b[i])) return false;
            return true;
        }
        [Obsolete]
        public static bool SequenceEqual<T>(Span<T> a, ReadOnlySpan<T> b)
        {
            return SequenceEqual((ReadOnlySpan<T>)a, b);
        }
    }
}";
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static bool F1(ReadOnlySpan<char> span) => span is ""123"";
    static bool F2(Span<char> span) => span is ""ABC"";
    static void F(Span<char> span)
    {
        Console.WriteLine((F1((ReadOnlySpan<char>)span), F2(span)));
    }
    static void Main()
    {
        F(new Span<char>(new [] { 'A', 'B', 'C' }));
        F(new Span<char>(new [] { '1', '2', '3' }));
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"(False, True)
(True, False)
");
        }

        [Fact]
        public void PatternMatchSpanChar_GetTypeInfo()
        {
            var source =
@"using System;
class Program
{
    static bool F1(ReadOnlySpan<char> span) => span is ""123"";
    static bool F2(Span<char> span) => span is ""ABC"";
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, exprs.Length);
            foreach (var expr in exprs)
            {
                var typeInfo = model.GetTypeInfo(expr);
                Assert.Equal(SpecialType.System_String, typeInfo.Type.SpecialType);
                Assert.Equal(SpecialType.System_String, typeInfo.ConvertedType.SpecialType);
            }
        }

        [Fact]
        public void PatternMatchSpanChar_GetDeclaredSymbol()
        {
            var source =
@"using System;
class Program
{
    static bool F1(ReadOnlySpan<char> span) => span is ""123"" and var r;
    static bool F2(Span<char> span) => span is ""ABC"" and var s;
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var locals = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            var types = locals.Select(local => ((ILocalSymbol)model.GetDeclaredSymbol(local)).Type.ToTestDisplayString()).ToArray();
            AssertEx.Equal(new[] { "System.ReadOnlySpan<System.Char>", "System.Span<System.Char>" }, types);
        }

        [Fact]
        public void PatternMatchSpanChar_IOperation_01()
        {
            var source =
@"using System;
class Program
{
    static bool F(ReadOnlySpan<char> span)
    {
        return span is ""123"";
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();

            var operation = model.GetOperation(syntax);
            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return span is ""123"";')
    ReturnedValue:
      IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'span is ""123""')
        Value:
          IParameterReferenceOperation: span (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Char>) (Syntax: 'span')
        Pattern:
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '""123""') (InputType: System.ReadOnlySpan<System.Char>, NarrowedType: System.ReadOnlySpan<System.Char>)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""123"") (Syntax: '""123""')
",
                actualText);

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(syntax, model);
            ControlFlowGraphVerifier.VerifyGraph(comp,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Return) Block[B2]
        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'span is ""123""')
          Value:
            IParameterReferenceOperation: span (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Char>) (Syntax: 'span')
          Pattern:
            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '""123""') (InputType: System.ReadOnlySpan<System.Char>, NarrowedType: System.ReadOnlySpan<System.Char>)
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""123"") (Syntax: '""123""')
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
",
                graph, symbol);
        }

        [Fact]
        public void PatternMatchSpanChar_IOperation_02()
        {
            var source =
@"using System;
class Program
{
    static bool F(Span<char> span)
    {
        return span is ""ABC"";
    }
}";
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();

            var operation = model.GetOperation(syntax);
            var actualText = OperationTreeVerifier.GetOperationTree(comp, operation);
            OperationTreeVerifier.Verify(
@"IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return span is ""ABC"";')
    ReturnedValue:
      IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'span is ""ABC""')
        Value:
          IParameterReferenceOperation: span (OperationKind.ParameterReference, Type: System.Span<System.Char>) (Syntax: 'span')
        Pattern:
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '""ABC""') (InputType: System.Span<System.Char>, NarrowedType: System.Span<System.Char>)
            Value:
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""ABC"") (Syntax: '""ABC""')
",
                actualText);

            var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(syntax, model);
            ControlFlowGraphVerifier.VerifyGraph(comp,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Return) Block[B2]
        IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'span is ""ABC""')
          Value:
            IParameterReferenceOperation: span (OperationKind.ParameterReference, Type: System.Span<System.Char>) (Syntax: 'span')
          Pattern:
            IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '""ABC""') (InputType: System.Span<System.Char>, NarrowedType: System.Span<System.Char>)
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""ABC"") (Syntax: '""ABC""')
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
",
                graph, symbol);
        }

        [Fact, WorkItem(50301, "https://github.com/dotnet/roslyn/issues/50301")]
        public void SymbolsForSwitchExpressionLocals()
        {
            var source = @"
class C
{
    static string M(object o)
    {
        return o switch
        {
            int i => $""Number: {i}"",
            _ => ""Don't know""
        };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            comp.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""10"" endColumn=""11"" document=""1"" />
        <entry offset=""0xf"" startLine=""8"" startColumn=""22"" endLine=""8"" endColumn=""36"" document=""1"" />
        <entry offset=""0x22"" startLine=""9"" startColumn=""18"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x28"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2a"">
        <scope startOffset=""0xf"" endOffset=""0x22"">
          <local name=""i"" il_index=""0"" il_start=""0xf"" il_end=""0x22"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact, WorkItem(59050, "https://github.com/dotnet/roslyn/issues/59050")]
        public void IsPatternInExceptionFilterInAsyncMethod_Spilled()
        {
            var source = @"
using System;
using System.Threading.Tasks;

static class C
{
    static async Task Main()
    {
        System.Console.Write(await ExceptionFilterBroken());
    }

    public static async Task<bool> ExceptionFilterBroken()
    {
        try
        {
            await ThrowException();
            return true;
        }
        catch (Exception ex) when (ex.InnerException is { Message: ""bad dog"" or ""dog bad"" })
        {
            return await TrueAsync();
        }
        catch
        {
            return false;
        }
    }

    static Task ThrowException() => throw new Exception("""", new Exception(""bad dog""));
    static Task<bool> TrueAsync() => Task.FromResult(true);
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            // Note: the important thing is that we now assign `System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3`
            // in the exception filter (at IL_00b6) before accessing `.InnerException` on it.
            verifier.VerifyIL("C.<ExceptionFilterBroken>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      471 (0x1d7)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Exception V_2,
                string V_3,
                bool V_4,
                System.Runtime.CompilerServices.TaskAwaiter V_5,
                C.<ExceptionFilterBroken>d__1 V_6,
                System.Exception V_7,
                int V_8,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_9)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0021
    IL_0014:  br         IL_0166
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.0
    IL_001c:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
    IL_0021:  nop
    .try
    {
      IL_0022:  ldloc.0
      IL_0023:  brfalse.s  IL_0027
      IL_0025:  br.s       IL_0029
      IL_0027:  br.s       IL_0068
      IL_0029:  nop
      IL_002a:  call       ""System.Threading.Tasks.Task C.ThrowException()""
      IL_002f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
      IL_0034:  stloc.s    V_5
      IL_0036:  ldloca.s   V_5
      IL_0038:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
      IL_003d:  brtrue.s   IL_0085
      IL_003f:  ldarg.0
      IL_0040:  ldc.i4.0
      IL_0041:  dup
      IL_0042:  stloc.0
      IL_0043:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
      IL_0048:  ldarg.0
      IL_0049:  ldloc.s    V_5
      IL_004b:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_0050:  ldarg.0
      IL_0051:  stloc.s    V_6
      IL_0053:  ldarg.0
      IL_0054:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
      IL_0059:  ldloca.s   V_5
      IL_005b:  ldloca.s   V_6
      IL_005d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<ExceptionFilterBroken>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<ExceptionFilterBroken>d__1)""
      IL_0062:  nop
      IL_0063:  leave      IL_01d6
      IL_0068:  ldarg.0
      IL_0069:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_006e:  stloc.s    V_5
      IL_0070:  ldarg.0
      IL_0071:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_0076:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_007c:  ldarg.0
      IL_007d:  ldc.i4.m1
      IL_007e:  dup
      IL_007f:  stloc.0
      IL_0080:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
      IL_0085:  ldloca.s   V_5
      IL_0087:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_008c:  nop
      IL_008d:  ldc.i4.1
      IL_008e:  stloc.1
      IL_008f:  leave      IL_01c1
    }
    filter
    {
      IL_0094:  isinst     ""System.Exception""
      IL_0099:  dup
      IL_009a:  brtrue.s   IL_00a0
      IL_009c:  pop
      IL_009d:  ldc.i4.0
      IL_009e:  br.s       IL_0106
      IL_00a0:  stloc.s    V_7
      IL_00a2:  ldarg.0
      IL_00a3:  ldloc.s    V_7
      IL_00a5:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
      IL_00aa:  ldarg.0
      IL_00ab:  ldarg.0
      IL_00ac:  ldfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
      IL_00b1:  castclass  ""System.Exception""
      IL_00b6:  stfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
      IL_00bb:  ldarg.0
      IL_00bc:  ldfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
      IL_00c1:  callvirt   ""System.Exception System.Exception.InnerException.get""
      IL_00c6:  stloc.2
      IL_00c7:  ldloc.2
      IL_00c8:  brfalse.s  IL_00f2
      IL_00ca:  ldloc.2
      IL_00cb:  callvirt   ""string System.Exception.Message.get""
      IL_00d0:  stloc.3
      IL_00d1:  ldloc.3
      IL_00d2:  ldstr      ""bad dog""
      IL_00d7:  call       ""bool string.op_Equality(string, string)""
      IL_00dc:  brtrue.s   IL_00ed
      IL_00de:  ldloc.3
      IL_00df:  ldstr      ""dog bad""
      IL_00e4:  call       ""bool string.op_Equality(string, string)""
      IL_00e9:  brtrue.s   IL_00ed
      IL_00eb:  br.s       IL_00f2
      IL_00ed:  ldc.i4.1
      IL_00ee:  stloc.s    V_4
      IL_00f0:  br.s       IL_00f5
      IL_00f2:  ldc.i4.0
      IL_00f3:  stloc.s    V_4
      IL_00f5:  ldarg.0
      IL_00f6:  ldloc.s    V_4
      IL_00f8:  stfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__4""
      IL_00fd:  ldarg.0
      IL_00fe:  ldfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__4""
      IL_0103:  ldc.i4.0
      IL_0104:  cgt.un
      IL_0106:  endfilter
    }  // end filter
    {  // handler
      IL_0108:  pop
      IL_0109:  ldarg.0
      IL_010a:  ldc.i4.1
      IL_010b:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
      IL_0110:  leave.s    IL_011b
    }
    catch object
    {
      IL_0112:  pop
      IL_0113:  nop
      IL_0114:  ldc.i4.0
      IL_0115:  stloc.1
      IL_0116:  leave      IL_01c1
    }
    IL_011b:  ldarg.0
    IL_011c:  ldfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
    IL_0121:  stloc.s    V_8
    IL_0123:  ldloc.s    V_8
    IL_0125:  ldc.i4.1
    IL_0126:  beq.s      IL_012a
    IL_0128:  br.s       IL_0199
    IL_012a:  nop
    IL_012b:  call       ""System.Threading.Tasks.Task<bool> C.TrueAsync()""
    IL_0130:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0135:  stloc.s    V_9
    IL_0137:  ldloca.s   V_9
    IL_0139:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_013e:  brtrue.s   IL_0183
    IL_0140:  ldarg.0
    IL_0141:  ldc.i4.1
    IL_0142:  dup
    IL_0143:  stloc.0
    IL_0144:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_0149:  ldarg.0
    IL_014a:  ldloc.s    V_9
    IL_014c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_0151:  ldarg.0
    IL_0152:  stloc.s    V_6
    IL_0154:  ldarg.0
    IL_0155:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
    IL_015a:  ldloca.s   V_9
    IL_015c:  ldloca.s   V_6
    IL_015e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<ExceptionFilterBroken>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<ExceptionFilterBroken>d__1)""
    IL_0163:  nop
    IL_0164:  leave.s    IL_01d6
    IL_0166:  ldarg.0
    IL_0167:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_016c:  stloc.s    V_9
    IL_016e:  ldarg.0
    IL_016f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_0174:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_017a:  ldarg.0
    IL_017b:  ldc.i4.m1
    IL_017c:  dup
    IL_017d:  stloc.0
    IL_017e:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_0183:  ldarg.0
    IL_0184:  ldloca.s   V_9
    IL_0186:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_018b:  stfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__5""
    IL_0190:  ldarg.0
    IL_0191:  ldfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__5""
    IL_0196:  stloc.1
    IL_0197:  leave.s    IL_01c1
    IL_0199:  ldarg.0
    IL_019a:  ldnull
    IL_019b:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
    IL_01a0:  ldarg.0
    IL_01a1:  ldnull
    IL_01a2:  stfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
    IL_01a7:  ldnull
    IL_01a8:  throw
  }
  catch System.Exception
  {
    IL_01a9:  stloc.2
    IL_01aa:  ldarg.0
    IL_01ab:  ldc.i4.s   -2
    IL_01ad:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_01b2:  ldarg.0
    IL_01b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
    IL_01b8:  ldloc.2
    IL_01b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.SetException(System.Exception)""
    IL_01be:  nop
    IL_01bf:  leave.s    IL_01d6
  }
  IL_01c1:  ldarg.0
  IL_01c2:  ldc.i4.s   -2
  IL_01c4:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
  IL_01c9:  ldarg.0
  IL_01ca:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
  IL_01cf:  ldloc.1
  IL_01d0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.SetResult(bool)""
  IL_01d5:  nop
  IL_01d6:  ret
}
");
        }

        [Fact, WorkItem(59050, "https://github.com/dotnet/roslyn/issues/59050")]
        public void IsPatternInExceptionFilterInAsyncMethod()
        {
            var source = @"
using System;
using System.Threading.Tasks;

static class C
{
    static async Task Main()
    {
        System.Console.Write(await ExceptionFilterBroken());
    }

    public static async Task<bool> ExceptionFilterBroken()
    {
        try
        {
            await ThrowException();
            return true;
        }
        catch (Exception ex) when (ex.InnerException is { Message: ""bad dog"" })
        {
            return await TrueAsync();
        }
        catch
        {
            return false;
        }
    }

    static Task ThrowException() => throw new Exception("""", new Exception(""bad dog""));
    static Task<bool> TrueAsync() => Task.FromResult(true);
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact, WorkItem(59050, "https://github.com/dotnet/roslyn/issues/59050")]
        public void IsPatternInExceptionFilterInAsyncMethod_ExecuteVariousCodePaths()
        {
            var source = @"
using System;
using System.Threading.Tasks;

Console.Write(await C.ExceptionFilterBroken(() => { }));
Console.Write(await C.ExceptionFilterBroken(() => C.ThrowException()));
Console.Write(await C.ExceptionFilterBroken(() => throw new Exception()));

public static class C
{
    public static async Task<int> ExceptionFilterBroken(Action a)
    {
        try
        {
            await Task.Yield();
            a();
            return 0;
        }
        catch (Exception ex) when (ex.InnerException is { Message: ""bad dog"" or ""dog bad"" })
        {
            return await OneAsync();
        }
        catch
        {
            return 2;
        }
    }

    public static void ThrowException() => throw new Exception("""", new Exception(""bad dog""));
    public static Task<int> OneAsync() => Task.FromResult(1);
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "012");
        }

        [Fact, WorkItem(59050, "https://github.com/dotnet/roslyn/issues/59050")]
        public void IsPatternInExceptionFilterInAsyncMethod_Spilled_NoExceptionLocal()
        {
            var source = @"
using System;
using System.Threading.Tasks;

static class C
{
    static async Task Main()
    {
        System.Console.Write(await ExceptionFilterBroken());
    }

    public static async Task<bool> ExceptionFilterBroken()
    {
        try
        {
            await ThrowException();
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                throw new Exception();
            }
            catch (Exception) when (ex.InnerException is { Message: ""bad dog"" or ""dog bad"" })
            {
                return await TrueAsync();
            }
        }
    }

    static Task ThrowException() => throw new Exception("""", new Exception(""bad dog""));
    static Task<bool> TrueAsync() => Task.FromResult(true);
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "True");
            verifier.VerifyIL("C.<ExceptionFilterBroken>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
 {
  // Code size      527 (0x20f)
  .maxstack  3
  .locals init (int V_0,
                bool V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<ExceptionFilterBroken>d__1 V_3,
                System.Exception V_4,
                int V_5,
                System.Exception V_6,
                string V_7,
                bool V_8,
                System.Exception V_9,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_10)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0021
    IL_0014:  br         IL_0192
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.0
    IL_001c:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
    IL_0021:  nop
    .try
    {
      IL_0022:  ldloc.0
      IL_0023:  brfalse.s  IL_0027
      IL_0025:  br.s       IL_0029
      IL_0027:  br.s       IL_0065
      IL_0029:  nop
      IL_002a:  call       ""System.Threading.Tasks.Task C.ThrowException()""
      IL_002f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
      IL_0034:  stloc.2
      IL_0035:  ldloca.s   V_2
      IL_0037:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
      IL_003c:  brtrue.s   IL_0081
      IL_003e:  ldarg.0
      IL_003f:  ldc.i4.0
      IL_0040:  dup
      IL_0041:  stloc.0
      IL_0042:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
      IL_0047:  ldarg.0
      IL_0048:  ldloc.2
      IL_0049:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_004e:  ldarg.0
      IL_004f:  stloc.3
      IL_0050:  ldarg.0
      IL_0051:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
      IL_0056:  ldloca.s   V_2
      IL_0058:  ldloca.s   V_3
      IL_005a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<ExceptionFilterBroken>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<ExceptionFilterBroken>d__1)""
      IL_005f:  nop
      IL_0060:  leave      IL_020e
      IL_0065:  ldarg.0
      IL_0066:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_006b:  stloc.2
      IL_006c:  ldarg.0
      IL_006d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<ExceptionFilterBroken>d__1.<>u__1""
      IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0078:  ldarg.0
      IL_0079:  ldc.i4.m1
      IL_007a:  dup
      IL_007b:  stloc.0
      IL_007c:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
      IL_0081:  ldloca.s   V_2
      IL_0083:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_0088:  nop
      IL_0089:  ldc.i4.1
      IL_008a:  stloc.1
      IL_008b:  leave      IL_01f9
    }
    catch System.Exception
    {
      IL_0090:  stloc.s    V_4
      IL_0092:  ldarg.0
      IL_0093:  ldloc.s    V_4
      IL_0095:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
      IL_009a:  ldarg.0
      IL_009b:  ldc.i4.1
      IL_009c:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
      IL_00a1:  leave.s    IL_00a3
    }
    IL_00a3:  ldarg.0
    IL_00a4:  ldfld      ""int C.<ExceptionFilterBroken>d__1.<>s__2""
    IL_00a9:  stloc.s    V_5
    IL_00ab:  ldloc.s    V_5
    IL_00ad:  ldc.i4.1
    IL_00ae:  beq.s      IL_00b5
    IL_00b0:  br         IL_01d6
    IL_00b5:  ldarg.0
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
    IL_00bc:  castclass  ""System.Exception""
    IL_00c1:  stfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
    IL_00c6:  nop
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.0
    IL_00c9:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__5""
    .try
    {
      IL_00ce:  nop
      IL_00cf:  newobj     ""System.Exception..ctor()""
      IL_00d4:  throw
    }
    filter
    {
      IL_00d5:  isinst     ""System.Exception""
      IL_00da:  dup
      IL_00db:  brtrue.s   IL_00e1
      IL_00dd:  pop
      IL_00de:  ldc.i4.0
      IL_00df:  br.s       IL_013c
      IL_00e1:  stloc.s    V_9
      IL_00e3:  ldarg.0
      IL_00e4:  ldloc.s    V_9
      IL_00e6:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__4""
      IL_00eb:  ldarg.0
      IL_00ec:  ldfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
      IL_00f1:  callvirt   ""System.Exception System.Exception.InnerException.get""
      IL_00f6:  stloc.s    V_6
      IL_00f8:  ldloc.s    V_6
      IL_00fa:  brfalse.s  IL_0128
      IL_00fc:  ldloc.s    V_6
      IL_00fe:  callvirt   ""string System.Exception.Message.get""
      IL_0103:  stloc.s    V_7
      IL_0105:  ldloc.s    V_7
      IL_0107:  ldstr      ""bad dog""
      IL_010c:  call       ""bool string.op_Equality(string, string)""
      IL_0111:  brtrue.s   IL_0123
      IL_0113:  ldloc.s    V_7
      IL_0115:  ldstr      ""dog bad""
      IL_011a:  call       ""bool string.op_Equality(string, string)""
      IL_011f:  brtrue.s   IL_0123
      IL_0121:  br.s       IL_0128
      IL_0123:  ldc.i4.1
      IL_0124:  stloc.s    V_8
      IL_0126:  br.s       IL_012b
      IL_0128:  ldc.i4.0
      IL_0129:  stloc.s    V_8
      IL_012b:  ldarg.0
      IL_012c:  ldloc.s    V_8
      IL_012e:  stfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__6""
      IL_0133:  ldarg.0
      IL_0134:  ldfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__6""
      IL_0139:  ldc.i4.0
      IL_013a:  cgt.un
      IL_013c:  endfilter
    }  // end filter
    {  // handler
      IL_013e:  pop
      IL_013f:  ldarg.0
      IL_0140:  ldc.i4.1
      IL_0141:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>s__5""
      IL_0146:  leave.s    IL_0148
    }
    IL_0148:  ldarg.0
    IL_0149:  ldfld      ""int C.<ExceptionFilterBroken>d__1.<>s__5""
    IL_014e:  stloc.s    V_5
    IL_0150:  ldloc.s    V_5
    IL_0152:  ldc.i4.1
    IL_0153:  beq.s      IL_0157
    IL_0155:  br.s       IL_01c5
    IL_0157:  nop
    IL_0158:  call       ""System.Threading.Tasks.Task<bool> C.TrueAsync()""
    IL_015d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0162:  stloc.s    V_10
    IL_0164:  ldloca.s   V_10
    IL_0166:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_016b:  brtrue.s   IL_01af
    IL_016d:  ldarg.0
    IL_016e:  ldc.i4.1
    IL_016f:  dup
    IL_0170:  stloc.0
    IL_0171:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_0176:  ldarg.0
    IL_0177:  ldloc.s    V_10
    IL_0179:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_017e:  ldarg.0
    IL_017f:  stloc.3
    IL_0180:  ldarg.0
    IL_0181:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
    IL_0186:  ldloca.s   V_10
    IL_0188:  ldloca.s   V_3
    IL_018a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<ExceptionFilterBroken>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<ExceptionFilterBroken>d__1)""
    IL_018f:  nop
    IL_0190:  leave.s    IL_020e
    IL_0192:  ldarg.0
    IL_0193:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_0198:  stloc.s    V_10
    IL_019a:  ldarg.0
    IL_019b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<ExceptionFilterBroken>d__1.<>u__2""
    IL_01a0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_01a6:  ldarg.0
    IL_01a7:  ldc.i4.m1
    IL_01a8:  dup
    IL_01a9:  stloc.0
    IL_01aa:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_01af:  ldarg.0
    IL_01b0:  ldloca.s   V_10
    IL_01b2:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_01b7:  stfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__7""
    IL_01bc:  ldarg.0
    IL_01bd:  ldfld      ""bool C.<ExceptionFilterBroken>d__1.<>s__7""
    IL_01c2:  stloc.1
    IL_01c3:  leave.s    IL_01f9
    IL_01c5:  ldarg.0
    IL_01c6:  ldnull
    IL_01c7:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__4""
    IL_01cc:  nop
    IL_01cd:  ldarg.0
    IL_01ce:  ldnull
    IL_01cf:  stfld      ""System.Exception C.<ExceptionFilterBroken>d__1.<ex>5__3""
    IL_01d4:  br.s       IL_01d6
    IL_01d6:  ldarg.0
    IL_01d7:  ldnull
    IL_01d8:  stfld      ""object C.<ExceptionFilterBroken>d__1.<>s__1""
    IL_01dd:  ldnull
    IL_01de:  throw
  }
  catch System.Exception
  {
    IL_01df:  stloc.s    V_6
    IL_01e1:  ldarg.0
    IL_01e2:  ldc.i4.s   -2
    IL_01e4:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
    IL_01e9:  ldarg.0
    IL_01ea:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
    IL_01ef:  ldloc.s    V_6
    IL_01f1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.SetException(System.Exception)""
    IL_01f6:  nop
    IL_01f7:  leave.s    IL_020e
  }
  IL_01f9:  ldarg.0
  IL_01fa:  ldc.i4.s   -2
  IL_01fc:  stfld      ""int C.<ExceptionFilterBroken>d__1.<>1__state""
  IL_0201:  ldarg.0
  IL_0202:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool> C.<ExceptionFilterBroken>d__1.<>t__builder""
  IL_0207:  ldloc.1
  IL_0208:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<bool>.SetResult(bool)""
  IL_020d:  nop
  IL_020e:  ret
}
");
        }

        [Fact]
        [WorkItem(63085, "https://github.com/dotnet/roslyn/issues/63085")]
        public void RefStructTypeTest_01()
        {
            CreateCompilation(@"
using System;

new G<int>().Test();
new G<object>().Test();
new G<int>().TestPattern();
new G<object>().TestPattern();

ref struct G<T>
{
    public void Test()
    {
        if (this is G<int>)
        {
            Console.WriteLine(""int"");
        }
        else if (this is G<object>)
        {
            Console.WriteLine(""object"");
        }
        else
        {
            Console.WriteLine(""unknown"");
            Console.WriteLine(typeof(T));
        }
    }

    public void TestPattern()
    {
        var genericTypePattern = this switch
        {
            G<int> => ""int"",
            G<object> => ""object"",
            _ => ""unknown""
        };

        Console.WriteLine(genericTypePattern);
    }
}
").VerifyDiagnostics(
                // (13,13): error CS0019: Operator 'is' cannot be applied to operands of type 'G<T>' and 'G<int>'
                //         if (this is G<int>)
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "this is G<int>").WithArguments("is", "G<T>", "G<int>").WithLocation(13, 13),
                // (17,18): error CS0019: Operator 'is' cannot be applied to operands of type 'G<T>' and 'G<object>'
                //         else if (this is G<object>)
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "this is G<object>").WithArguments("is", "G<T>", "G<object>").WithLocation(17, 18),
                // (32,13): error CS8121: An expression of type 'G<T>' cannot be handled by a pattern of type 'G<int>'.
                //             G<int> => "int",
                Diagnostic(ErrorCode.ERR_PatternWrongType, "G<int>").WithArguments("G<T>", "G<int>").WithLocation(32, 13),
                // (33,13): error CS8121: An expression of type 'G<T>' cannot be handled by a pattern of type 'G<object>'.
                //             G<object> => "object",
                Diagnostic(ErrorCode.ERR_PatternWrongType, "G<object>").WithArguments("G<T>", "G<object>").WithLocation(33, 13)
                );
        }

        [Fact]
        [WorkItem(63085, "https://github.com/dotnet/roslyn/issues/63085")]
        public void RefStructTypeTest_02()
        {
            CreateCompilation(@"
ref struct G<T> where T : class
{
    public void Test1(T x1)
    {
        var y1 = x1 as G<object>;
    }

    public void Test2(G<object> x2)
    {
        var y2 = x2 as T;
    }
}
").VerifyDiagnostics(
                // (6,18): error CS0077: The as operator must be used with a reference type or nullable type ('G<object>' is a non-nullable value type)
                //         var y1 = x1 as G<object>;
                Diagnostic(ErrorCode.ERR_AsMustHaveReferenceType, "x1 as G<object>").WithArguments("G<object>").WithLocation(6, 18),
                // (11,18): error CS0019: Operator 'as' cannot be applied to operands of type 'G<object>' and 'T'
                //         var y2 = x2 as T;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x2 as T").WithArguments("as", "G<object>", "T").WithLocation(11, 18)
                );
        }

        [Fact]
        [WorkItem(63476, "https://github.com/dotnet/roslyn/issues/63476")]
        public void PatternNonConstant_UserDefinedImplicit_ConversionToInputType()
        {
            var source =
@"
class A {
    public string S { get; set; }
    public static implicit operator A(string s) { return new A { S = s }; }
}
class C
{
    static bool M(A a) => a switch { ""implicitA"" => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (8,38): error CS9133: A constant value of type 'A' is expected
                    //     static bool M(A a) => a switch { "implicitA" => true, _ => false };
                    Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"""implicitA""").WithArguments("A").WithLocation(8, 38)
                );
        }

        [Fact]
        [WorkItem(63476, "https://github.com/dotnet/roslyn/issues/63476")]
        public void PatternNonConstant_UserDefinedExplicit_ConversionToInputType()
        {
            var source =
@"
class A {
    public string S { get; set; }
    public static implicit operator A(string s) { return new A { S = s }; }
}
class C
{
    static bool M(A a) => a switch { (A)""castedA"" => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (8,38): error CS9133: A constant value of type 'A' is expected
                    //     static bool M(A a) => a switch { (A)"castedA" => true, _ => false };
                    Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"(A)""castedA""").WithArguments("A").WithLocation(8, 38)
                );
        }

        [Fact]
        public void PatternReadOnlySpan_ImplicitBuiltInConversion_ToString()
        {
            var source =
@"
using System;
class C
{
    static bool M(ReadOnlySpan<char> chars) => chars switch { """" => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(); // Allowed due to built in conversion
        }

        [Fact]
        public void PatternNoImplicitConversionToInputType()
        {
            // Cannot implicitly cast long to byte..
            var source =
@"
class C
{
    static bool M(byte b) => b switch { 1L => true, _ => false };
}";
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (4,41): error CS0266: Cannot implicitly convert type 'long' to 'byte'. An explicit conversion exists (are you missing a cast?)
                    //     static bool M(byte b) => b switch { 1l => true, _ => false };
                    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "byte").WithLocation(4, 41));
        }
    }
}
