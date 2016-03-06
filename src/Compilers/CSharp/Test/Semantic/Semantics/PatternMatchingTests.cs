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
        private static CSharpParseOptions patternParseOptions =
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)
                    .WithFeature("patterns", "true")
                    .WithFeature("patternsExperimental", "true");

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                );
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

        [Fact]
        public void PropertyPatternTest()
        {
            var source =
@"using System;
public class Expression {}
public class Constant : Expression
{
    public readonly int Value;
    public Constant(int Value)
    {
        this.Value = Value;
    }
}
public class Plus : Expression
{
    public readonly Expression Left, Right;
    public Plus(Expression Left, Expression Right)
    {
        this.Left = Left;
        this.Right = Right;
    }
}
public class X
{
    public static void Main()
    {
        // ((1 + (2 + 3)) + 6)
        Expression expr = new Plus(new Plus(new Constant(1), new Plus(new Constant(2), new Constant(3))), new Constant(6));
        // The recursive form of this pattern would be 
        //  expr is Plus(Plus(Constant(int x1), Plus(Constant(int x2), Constant(int x3))), Constant(int x6))
        if (expr is Plus { Left is Plus { Left is Constant { Value is int x1 }, Right is Plus { Left is Constant { Value is int x2 }, Right is Constant { Value is int x3 } } }, Right is Constant { Value is int x6 } })
        {
            Console.WriteLine(""{0} {1} {2} {3}"", x1, x2, x3, x6);
        }
        else
        {
            Console.WriteLine(""wrong"");
        }
        Console.WriteLine(expr is Plus { Left is Plus { Left is Constant { Value is 1 }, Right is Plus { Left is Constant { Value is 2 }, Right is Constant { Value is 3 } } }, Right is Constant { Value is 6 } });
    }
}";
            var expectedOutput =
@"1 2 3 6
True";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void InferredPositionalPatternTest()
        {
            var source =
@"using System;
public class Expression {}
public class Constant : Expression
{
    public readonly int Value;
    public Constant(int Value)
    {
        this.Value = Value;
    }
}
public class Plus : Expression
{
    public readonly Expression Left, Right;
    public Plus(Expression Left, Expression Right)
    {
        this.Left = Left;
        this.Right = Right;
    }
}
public class X
{
    public static void Main()
    {
        // ((1 + (2 + 3)) + 6)
        Expression expr = new Plus(new Plus(new Constant(1), new Plus(new Constant(2), new Constant(3))), new Constant(6));
        // The recursive form of this pattern would be 
        if (expr is Plus(Plus(Constant(int x1), Plus(Constant(int x2), Constant(int x3))), Constant(int x6)))
        {
            Console.WriteLine(""{0} {1} {2} {3}"", x1, x2, x3, x6);
        }
        else
        {
            Console.WriteLine(""wrong"");
        }
        Console.WriteLine(expr is Plus(Plus(Constant(1), Plus(Constant(2), Constant(3))), Constant(6)));
        Console.WriteLine(expr is Plus(Plus(Constant(1), Plus(Constant(2), Constant(4))), Constant(6)));
    }
}";
            var expectedOutput =
@"1 2 3 6
True
False";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
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
        if (s is string t) { } else Console.WriteLine(t); // t not in scope
        if (null is dynamic t) { } // null not allowed
        if (s is NullableInt x) { } // error: cannot use nullable type
        if (s is long l) { } // error: cannot convert string to long
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (8,55): error CS0103: The name 't' does not exist in the current context
                //         if (s is string t) { } else Console.WriteLine(t); // t not in scope
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(8, 55),
                // (9,13): error CS8098: Invalid operand for pattern match.
                //         if (null is dynamic t) { } // null not allowed
                Diagnostic(ErrorCode.ERR_BadIsPatternExpression, "null").WithLocation(9, 13),
                // (10,18): error CS8097: It is not legal to use nullable type 'int?' in a pattern; use the underlying type 'int' instead.
                //         if (s is NullableInt x) { } // error: cannot use nullable type
                Diagnostic(ErrorCode.ERR_PatternNullableType, "NullableInt").WithArguments("int?", "int").WithLocation(10, 18),
                // (11,18): error CS0030: Cannot convert type 'string' to 'long'
                //         if (s is long l) { } // error: cannot convert string to long
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "long").WithArguments("string", "long").WithLocation(11, 18)
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"No for 1
Yes for 10
No for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False for 1
True for 10
False for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False for 1
True for 10
False for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8778")]
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithFeature("localFunctions", "true"));
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False for 1
True for 10
False for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8778")]
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
        B(o1);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False for 1
True for 10
False for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
    // (2,28): error CS0103: The name 's' does not exist in the current context
    // [Obsolete("" is string s ? s : "")]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(2, 28),
    // (8,55): error CS0103: The name 'o' does not exist in the current context
    //     private static void M(string p = "" is object o ? o.ToString() : "")
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(8, 55),
    // (8,34): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'string'
    //     private static void M(string p = "" is object o ? o.ToString() : "")
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "string").WithLocation(8, 34)
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False for 1
True for 10
False for 1.2";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
        public void MatchExpression00()
        {
            var source =
@"using System;
public struct X
{
    static void Main(string[] args)
    {
        Person[] oa = {
            new Student(""Einstein"", 4.0),
            new Student(""Elvis"", 3.0),
            new Student(""Poindexter"", 3.2),
            new Teacher(""Feynmann"", ""Physics""),
            new Person(""Anders""),
        };
        foreach (var o in oa)
        {
            Console.WriteLine(PrintedForm(o));
        }
        //Console.ReadKey();
    }
    static string PrintedForm(Person p) => p match (
        case Student s when s.Gpa > 3.5 :
            $""Honor Student { s.Name } ({ s.Gpa :N1})""
        case Student { Name is ""Poindexter"" } :
            ""A Nerd""
        case Student s :
            $""Student {s.Name} ({s.Gpa:N1})""
        case Teacher t :
            $""Teacher {t.Name} of {t.Subject}""
        case null :
            throw new ArgumentNullException(nameof(p))
        case * :
            $""Person {p.Name}""
        );
}
// class Person(string Name);
class Person
{
    public Person(string name) { this.Name = name; }
    public string Name { get; }
}

// class Student(string Name, double Gpa) : Person(Name);
class Student : Person
{
    public Student(string name, double gpa) : base(name)
        { this.Gpa = gpa; }
    public double Gpa { get; }
}

// class Teacher(string Name, string Subject) : Person(Name);
class Teacher : Person
{
    public Teacher(string name, string subject) : base(name)
        { this.Subject = subject; }
    public string Subject { get; }
}

";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"Honor Student Einstein (4.0)
Student Elvis (3.0)
A Nerd
Teacher Feynmann of Physics
Person Anders
";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void LetStatement00()
        {
            var source =
@"using System;
public struct X
{
    static void M(object o1, X o2, int? o3)
    {
        let string s1 = o1 when s1.Length > 0
            else { Console.WriteLine(""o1 is empty""); return; }
        let s2 = s1;
        Console.WriteLine(s2);
        let X { Z is int z, W is int w } = o2;
        Console.WriteLine(z);
        Console.WriteLine(w);
        let int i = o3
            else { Console.WriteLine(""o3 is null""); return; }
        Console.WriteLine(i);
    }
    static void Main(string[] args)
    {
        X x = new X();
        M(null, x, null);
        M("""", x, null);
        M(""foo"", x, null);
        M(""foo"", x, 321);
    }
    public int Z => 12;
    public int W => 23;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"o1 is empty
o1 is empty
foo
12
23
o3 is null
foo
12
23
321
";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void LetStatement01()
        {
            var source =
@"using System;
public class X
{
    public static void Main() {}
    static void M(object o1, X o2, int? o3)
    {
        let string s1 = o1
            else { Console.WriteLine(""o1 is empty""); }
        let s2 = s1; // error: s1 not definitely assigned
        Console.WriteLine(s2);
        let X { Z is int z, W is int w } = o2;
        Console.WriteLine(z); // error
        Console.WriteLine(w); // error
        let int i = o3
            else { Console.WriteLine(""o3 is null""); }
        Console.WriteLine(i); // error
    }
    public int Z => 12;
    public int W => 23;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (9,18): error CS0165: Use of unassigned local variable 's1'
                //         let s2 = s1; // error: s1 not definitely assigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(9, 18),
                // (12,27): error CS0165: Use of unassigned local variable 'z'
                //         Console.WriteLine(z); // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(12, 27),
                // (13,27): error CS0165: Use of unassigned local variable 'w'
                //         Console.WriteLine(w); // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "w").WithArguments("w").WithLocation(13, 27),
                // (16,27): error CS0165: Use of unassigned local variable 'i'
                //         Console.WriteLine(i); // error
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(16, 27)
                );
        }

        [Fact]
        public void PatternVariablesAreReadonly()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        let x = 12;
        x = x + 1; // error: x is readonly
        x++;       // error: x is readonly
        M1(ref x); // error: x is readonly
        M2(out x); // error: x is readonly
    }
    public static void M1(ref int x) {}
    public static void M2(out int x) { x = 1; }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x = x + 1; // error: x is readonly
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x").WithLocation(7, 9),
                // (8,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         x++;       // error: x is readonly
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "x").WithLocation(8, 9),
                // (9,16): error CS1510: A ref or out argument must be an assignable variable
                //         M1(ref x); // error: x is readonly
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(9, 16),
                // (10,16): error CS1510: A ref or out argument must be an assignable variable
                //         M2(out x); // error: x is readonly
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(10, 16)
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

    void Test6()
    {
        let x6 = 11;
        Dummy(x6);
        Dummy(true is var x6, x6);
    }

    void Test7()
    {
        Dummy(true is var x7, x7);
        let x7 = 11;
        Dummy(x7);
    }

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

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
    // (47,27): error CS0136: A local or parameter named 'x6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x6);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x6").WithArguments("x6").WithLocation(47, 27),
    // (52,27): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x7);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(52, 27),
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

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);

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

        [Fact]
        public void PropertyNamedInComplexPattern()
        {
            var source =
@"
using System;
public class Program
{
    public static void Main()
    {
        object o = nameof(Main);
        Console.WriteLine(o is P { Good is 4 });
        Console.WriteLine(o is P { NotFound is 4 });
        Console.WriteLine(o is P { Unreadable1 is 4 });
        Console.WriteLine(o is P { Unreadable2 is 4 });

    }
}
class P
{
    public int Good = 2;
    public int Unreadable1 { set { } }
    public int Unreadable2 { set { } protected get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

            compilation.VerifyDiagnostics(
                // (9,36): error CS0117: 'P' does not contain a definition for 'NotFound'
                //         Console.WriteLine(o is P { NotFound is 4 });
                Diagnostic(ErrorCode.ERR_NoSuchMember, "NotFound").WithArguments("P", "NotFound").WithLocation(9, 36),
                // (10,36): error CS0154: The property or indexer 'P.Unreadable1' cannot be used in this context because it lacks the get accessor
                //         Console.WriteLine(o is P { Unreadable1 is 4 });
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "Unreadable1").WithArguments("P.Unreadable1").WithLocation(10, 36),
                // (11,36): error CS0271: The property or indexer 'P.Unreadable2' cannot be used in this context because the get accessor is inaccessible
                //         Console.WriteLine(o is P { Unreadable2 is 4 });
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "Unreadable2").WithArguments("P.Unreadable2").WithLocation(11, 36)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var propPats = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Where(e => e.Parent is PropertyPatternSyntax).ToArray();
            Assert.Equal(4, propPats.Length);

            var p = propPats[0].Expression; // 'Good' in Good is 4
            var si = model.GetSymbolInfo(p);
            Assert.NotNull(si.Symbol);
            Assert.Equal("Good", si.Symbol.Name);
            Assert.Equal(CandidateReason.None, si.CandidateReason);
            Assert.True(si.CandidateSymbols.IsDefaultOrEmpty);

            p = propPats[1].Expression; // 'NotFound' in NotFound is 4
            si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(CandidateReason.None, si.CandidateReason);
            Assert.True(si.CandidateSymbols.IsDefaultOrEmpty);

            p = propPats[2].Expression; // 'Unreadable1' in Unreadable1 is 4
            si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(CandidateReason.NotAValue, si.CandidateReason);
            Assert.Equal(1, si.CandidateSymbols.Length);
            Assert.Equal("Unreadable1", si.CandidateSymbols[0].Name);

            p = propPats[3].Expression; // 'Unreadable2' in Unreadable2 is 4
            si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(CandidateReason.NotAValue, si.CandidateReason);
            Assert.Equal(1, si.CandidateSymbols.Length);
            Assert.Equal("Unreadable2", si.CandidateSymbols[0].Name);
        }

        [Fact, WorkItem(9284, "https://github.com/dotnet/roslyn/issues/9284")]
        public void AmbiguousNamedProperty()
        {
            var source =
@"
using System;
public class Program
{
    public static void Main()
    {
        object o = null;
        Console.WriteLine(o is I3 { Property is 4 });
    }
}
interface I1
{
    int Property { get; }
}
interface I2
{
    int Property { get; }
}
interface I3 : I1, I2 { }
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

            compilation.VerifyDiagnostics(
                // (8,37): error CS0229: Ambiguity between 'I1.Property' and 'I2.Property'
                //         Console.WriteLine(o is I3 { Property is 4 });
                Diagnostic(ErrorCode.ERR_AmbigMember, "Property").WithArguments("I1.Property", "I2.Property").WithLocation(8, 37)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var propPats = tree.GetRoot().DescendantNodes().OfType<PropertyPatternSyntax>().SelectMany(s => s.SubPatterns).OfType<IsPatternExpressionSyntax>().ToArray();
            Assert.Equal(1, propPats.Length);

            var p = propPats[0].Expression; // 'Property' in "Property is 4"
            var si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, si.CandidateReason);
            Assert.Equal(2, si.CandidateSymbols.Length);
            Assert.Equal("I1", si.CandidateSymbols[0].ContainingSymbol.Name);
            Assert.Equal("I2", si.CandidateSymbols[1].ContainingSymbol.Name);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9284"), WorkItem(9284, "https://github.com/dotnet/roslyn/issues/9284")]
        public void StaticNamedProperty()
        {
            var source =
@"
using System;
public class Program
{
    public static void Main()
    {
        object o = null;
        Console.WriteLine(o is Point { X is 4 });
    }
}
class Point
{
    public static int X => 0;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

            // TODO: need a better diagnostic for this
            compilation.VerifyDiagnostics(
                // (8,40): error CS0176: Member 'Point.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         Console.WriteLine(o is Point { X is 4 });
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("Point.X").WithLocation(8, 40)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var propPats = tree.GetRoot().DescendantNodes().OfType<PropertyPatternSyntax>().SelectMany(s => s.SubPatterns).OfType<IsPatternExpressionSyntax>().ToArray();
            Assert.Equal(1, propPats.Length);

            var p = propPats[0].Expression; // 'X' in "X is 4"
            var si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(1, si.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.StaticInstanceMismatch, si.CandidateReason);
        }

        [Fact]
        public void InaccessibleNamedProperty()
        {
            var source =
@"
using System;
public class Program
{
    public static void Main()
    {
        object o = null;
        Console.WriteLine(o is Point { X is 4 });
    }
}
class Point
{
    protected int X => 0;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

            // TODO: need a better diagnostic for this
            compilation.VerifyDiagnostics(
                // (8,40): error CS0122: 'Point.X' is inaccessible due to its protection level
                //         Console.WriteLine(o is Point { X is 4 });
                Diagnostic(ErrorCode.ERR_BadAccess, "X").WithArguments("Point.X").WithLocation(8, 40)
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var propPats = tree.GetRoot().DescendantNodes().OfType<PropertyPatternSyntax>().SelectMany(s => s.SubPatterns).OfType<IsPatternExpressionSyntax>().ToArray();
            Assert.Equal(1, propPats.Length);

            var p = propPats[0].Expression; // 'X' in "X is 4"
            var si = model.GetSymbolInfo(p);
            Assert.Null(si.Symbol);
            Assert.Equal(1, si.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.Inaccessible, si.CandidateReason);
        }

        private static void VerifyModelForDeclarationPattern(SemanticModel model, DeclarationPatternSyntax decl, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

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
        public void ScopeOfPatternVariables_Let_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1()
    {
        let x1 = 12;
        var y = x1;
    }

    void Test2()
    {
        var y = x2;
        let x2 = 12;
    }

    void Test3()
    {
        var x3 = 11;
        let x3 = 12;
        var y = x3;
    }

    void Test4()
    {
        let x4 = 12;
        var x4 = 11;
        var y = x4;
    }

    void Test5()
    {
        let x5 = 11;
        let x5 = 12;
        var y = x5;
    }

    void Test6()
    {
        {
            let x6 = 12;
            var y = x6;
        }

        {
            let x6 = 12;
            var y = x6;
        }
    }

    void Test7()
    {
        System.Console.WriteLine(x7);

        {
            let x7 = 12;
            var y = x7;
        }
    }

    void Test8()
    {
        {
            let x8 = 12;
            var y = x8;
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        var x9 = 11;

        {
            let x9 = 12;
            var y = x9;
        }

        System.Console.WriteLine(x9);
    }

    void Test10()
    {
        {
            let x10 = 12;
            var y = x10;
        }

        var x10 = 11;
        System.Console.WriteLine(x10);
    }

    void Test11()
    {
        let x11 = 11;

        {
            var x11 = 12;
            var y = x11;
        }

        System.Console.WriteLine(x11);
    }

    void Test12()
    {
        {
            var x12 = 12;
            var y = x12;
        }

        let x12 = 11;
        System.Console.WriteLine(x12);
    }

    void Test13()
    {
        let x13 = 11;

        {
            let x13 = 12;
            var y = x13;
        }

        System.Console.WriteLine(x13);
    }

    void Test14()
    {
        {
            let x14 = 12;
            var y = x14;
        }

        let x14 = 11;
        System.Console.WriteLine(x14);
    }

    void Test15(int x15, int y15)
    {
        {
            let y15 = 12;
            var y = y15;
        }

        let x15 = 11;
        System.Console.WriteLine(x15);
    }

    void Test16(int x16) => let x16 = 11;

    void Test17()
    {
        void Test(int x17) => let x17 = 11;
        Test(1);
    }

    void Test18()
    {
        if (true)
            var x18 = 11;

        if (y18)
            let y18 = 11;

        System.Console.WriteLine(y18);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());
            compilation.VerifyDiagnostics(
    // (154,33): error CS1002: ; expected
    //     void Test16(int x16) => let x16 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x16").WithLocation(154, 33),
    // (154,37): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test16(int x16) => let x16 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(154, 37),
    // (154,37): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test16(int x16) => let x16 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(154, 37),
    // (158,35): error CS1002: ; expected
    //         void Test(int x17) => let x17 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x17").WithLocation(158, 35),
    // (165,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var x18 = 11;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var x18 = 11;").WithLocation(165, 13),
    // (168,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             let y18 = 11;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "let y18 = 11;").WithLocation(168, 13),
    // (16,17): error CS0841: Cannot use local variable 'x2' before it is declared
    //         var y = x2;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(16, 17),
    // (23,13): error CS0128: A local variable named 'x3' is already defined in this scope
    //         let x3 = 12;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(23, 13),
    // (30,13): error CS0128: A local variable named 'x4' is already defined in this scope
    //         var x4 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(30, 13),
    // (30,13): warning CS0219: The variable 'x4' is assigned but its value is never used
    //         var x4 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(30, 13),
    // (37,13): error CS0128: A local variable named 'x5' is already defined in this scope
    //         let x5 = 12;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(37, 13),
    // (56,34): error CS0103: The name 'x7' does not exist in the current context
    //         System.Console.WriteLine(x7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(56, 34),
    // (71,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(71, 34),
    // (79,17): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let x9 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(79, 17),
    // (89,17): error CS0136: A local or parameter named 'x10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let x10 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x10").WithArguments("x10").WithLocation(89, 17),
    // (102,17): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x11 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(102, 17),
    // (112,17): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x12 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(112, 17),
    // (125,17): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let x13 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(125, 17),
    // (135,17): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let x14 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(135, 17),
    // (146,17): error CS0136: A local or parameter named 'y15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let y15 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y15").WithArguments("y15").WithLocation(146, 17),
    // (150,13): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         let x15 = 11;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(150, 13),
    // (154,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test16(int x16) => let x16 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(154, 29),
    // (154,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test16(int x16) => let x16 = 11;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(154, 29),
    // (158,31): error CS0103: The name 'let' does not exist in the current context
    //         void Test(int x17) => let x17 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(158, 31),
    // (158,31): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void Test(int x17) => let x17 = 11;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(158, 31),
    // (158,35): error CS0103: The name 'x17' does not exist in the current context
    //         void Test(int x17) => let x17 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x17").WithArguments("x17").WithLocation(158, 35),
    // (167,13): error CS0103: The name 'y18' does not exist in the current context
    //         if (y18)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y18").WithArguments("y18").WithLocation(167, 13),
    // (170,34): error CS0103: The name 'y18' does not exist in the current context
    //         System.Console.WriteLine(y18);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y18").WithArguments("y18").WithLocation(170, 34)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x5").ToArray();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x6").ToArray();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").ToArray();
            Assert.Equal(2, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref[0]);
            VerifyNotAPatternLocal(model, x10Ref[1]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyNotAPatternLocal(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x13").ToArray();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(2, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[1]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[0]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref[0]);
            VerifyModelForDeclarationPattern(model, x14Decl[1], x14Ref[1]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x15").Single();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").Single();
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref);

            var y15Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "y15").Single();
            var y15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y15").Single();
            VerifyModelForDeclarationPattern(model, y15Decl, y15Ref);

            Assert.False(tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x16").Any());

            Assert.False(tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "x17").Any());

            var x18Decl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(p => p.Identifier.ValueText == "x18").Single();
            Assert.Equal("x18", model.GetDeclaredSymbol(x18Decl).Name);

            var y18Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "y18").Single();
            var y18Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y18").ToArray();
            Assert.Equal(2, y18Ref.Length);
            VerifyNotInScope(model, y18Ref[0]);
            VerifyNotInScope(model, y18Ref[1]);
            VerifyModelForDeclarationPattern(model, y18Decl);
        }

        private static void VerifyModelForDeclarationPattern(SemanticModel model, LetStatementSyntax decl, params IdentifierNameSyntax[] references)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.Same(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));

            foreach (var reference in references)
            {
                Assert.Same(symbol, model.GetSymbolInfo(reference).Symbol);
                Assert.Same(symbol, model.LookupSymbols(reference.SpanStart, name: decl.Identifier.ValueText).Single());
                Assert.True(model.LookupNames(reference.SpanStart).Contains(decl.Identifier.ValueText));
            }
        }

        private static void VerifyModelForDeclarationPatternDuplicateInSameScope(SemanticModel model, LetStatementSyntax decl)
        {
            var symbol = model.GetDeclaredSymbol(decl);
            Assert.Equal(decl.Identifier.ValueText, symbol.Name);
            Assert.Equal(LocalDeclarationKind.PatternVariable, ((LocalSymbol)symbol).DeclarationKind);
            Assert.Same(symbol, model.GetDeclaredSymbol((SyntaxNode)decl));
            Assert.NotEqual(symbol, model.LookupSymbols(decl.SpanStart, name: decl.Identifier.ValueText).Single());
            Assert.True(model.LookupNames(decl.SpanStart).Contains(decl.Identifier.ValueText));
        }

        [Fact]
        public void ScopeOfPatternVariables_Let_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1()
    {
        let var x1 = 12;
        var y = x1;
    }

    void Test2()
    {
        var y = x2;
        let var x2 = 12;
    }

    void Test3()
    {
        var x3 = 11;
        let var x3 = 12;
        var y = x3;
    }

    void Test4()
    {
        let var x4 = 12;
        var x4 = 11;
        var y = x4;
    }

    void Test5()
    {
        let var x5 = 11;
        let var x5 = 12;
        var y = x5;
    }

    void Test6()
    {
        {
            let var x6 = 12;
            var y = x6;
        }

        {
            let var x6 = 12;
            var y = x6;
        }
    }

    void Test7()
    {
        System.Console.WriteLine(x7);

        {
            let var x7 = 12;
            var y = x7;
        }
    }

    void Test8()
    {
        {
            let var x8 = 12;
            var y = x8;
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        var x9 = 11;

        {
            let var x9 = 12;
            var y = x9;
        }

        System.Console.WriteLine(x9);
    }

    void Test10()
    {
        {
            let var x10 = 12;
            var y = x10;
        }

        var x10 = 11;
        System.Console.WriteLine(x10);
    }

    void Test11()
    {
        let var x11 = 11;

        {
            var x11 = 12;
            var y = x11;
        }

        System.Console.WriteLine(x11);
    }

    void Test12()
    {
        {
            var x12 = 12;
            var y = x12;
        }

        let var x12 = 11;
        System.Console.WriteLine(x12);
    }

    void Test13()
    {
        let var x13 = 11;

        {
            let var x13 = 12;
            var y = x13;
        }

        System.Console.WriteLine(x13);
    }

    void Test14()
    {
        {
            let var x14 = 12;
            var y = x14;
        }

        let var x14 = 11;
        System.Console.WriteLine(x14);
    }

    void Test15(int x15, int y15)
    {
        {
            let var y15 = 12;
            var y = y15;
        }

        let var x15 = 11;
        System.Console.WriteLine(x15);
    }

    void Test16(int x16) => let var x16 = 11;

    void Test17()
    {
        void Test(int x17) => let var x17 = 11;
        Test(1);
    }

    void Test18()
    {
        if (true)
            var x18 = 11;

        if (y18)
            let var y18 = 11;

        System.Console.WriteLine(y18);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());
            compilation.VerifyDiagnostics(
    // (154,33): error CS1002: ; expected
    //     void Test16(int x16) => let var x16 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(154, 33),
    // (158,35): error CS1002: ; expected
    //         void Test(int x17) => let var x17 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(158, 35),
    // (165,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var x18 = 11;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var x18 = 11;").WithLocation(165, 13),
    // (168,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             let var y18 = 11;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "let var y18 = 11;").WithLocation(168, 13),
    // (154,33): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     void Test16(int x16) => let var x16 = 11;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(154, 33),
    // (16,17): error CS0841: Cannot use local variable 'x2' before it is declared
    //         var y = x2;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(16, 17),
    // (23,17): error CS0128: A local variable named 'x3' is already defined in this scope
    //         let var x3 = 12;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(23, 17),
    // (30,13): error CS0128: A local variable named 'x4' is already defined in this scope
    //         var x4 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(30, 13),
    // (30,13): warning CS0219: The variable 'x4' is assigned but its value is never used
    //         var x4 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x4").WithArguments("x4").WithLocation(30, 13),
    // (37,17): error CS0128: A local variable named 'x5' is already defined in this scope
    //         let var x5 = 12;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(37, 17),
    // (56,34): error CS0103: The name 'x7' does not exist in the current context
    //         System.Console.WriteLine(x7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(56, 34),
    // (71,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(71, 34),
    // (79,21): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let var x9 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(79, 21),
    // (89,21): error CS0136: A local or parameter named 'x10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let var x10 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x10").WithArguments("x10").WithLocation(89, 21),
    // (102,17): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x11 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(102, 17),
    // (112,17): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x12 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(112, 17),
    // (125,21): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let var x13 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(125, 21),
    // (135,21): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let var x14 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(135, 21),
    // (146,21): error CS0136: A local or parameter named 'y15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             let var y15 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y15").WithArguments("y15").WithLocation(146, 21),
    // (150,17): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         let var x15 = 11;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(150, 17),
    // (154,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test16(int x16) => let var x16 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(154, 29),
    // (154,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test16(int x16) => let var x16 = 11;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(154, 29),
    // (158,31): error CS0103: The name 'let' does not exist in the current context
    //         void Test(int x17) => let var x17 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(158, 31),
    // (158,31): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void Test(int x17) => let var x17 = 11;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(158, 31),
    // (158,23): error CS0136: A local or parameter named 'x17' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Test(int x17) => let var x17 = 11;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x17").WithArguments("x17").WithLocation(158, 23),
    // (158,39): warning CS0219: The variable 'x17' is assigned but its value is never used
    //         void Test(int x17) => let var x17 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x17").WithArguments("x17").WithLocation(158, 39),
    // (167,13): error CS0103: The name 'y18' does not exist in the current context
    //         if (y18)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y18").WithArguments("y18").WithLocation(167, 13),
    // (170,34): error CS0103: The name 'y18' does not exist in the current context
    //         System.Console.WriteLine(y18);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y18").WithArguments("y18").WithLocation(170, 34)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref);

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
            VerifyNotInScope(model, x7Ref[0]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0]);
            VerifyNotInScope(model, x8Ref[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").ToArray();
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").ToArray();
            Assert.Equal(2, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref[0]);
            VerifyNotAPatternLocal(model, x10Ref[1]);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyNotAPatternLocal(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1]);

            var x13Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x13").ToArray();
            var x13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(2, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[1]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[0]);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref[0]);
            VerifyModelForDeclarationPattern(model, x14Decl[1], x14Ref[1]);

            var x15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x15").Single();
            var x15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x15").Single();
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref);

            var y15Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y15").Single();
            var y15Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y15").Single();
            VerifyModelForDeclarationPattern(model, y15Decl, y15Ref);

            Assert.False(tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x16").Any());

            Assert.False(tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x17").Any());

            var x18Decl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(p => p.Identifier.ValueText == "x18").Single();
            Assert.Equal("x18", model.GetDeclaredSymbol(x18Decl).Name);

            var y18Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y18").Single();
            var y18Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y18").ToArray();
            Assert.Equal(2, y18Ref.Length);
            VerifyNotInScope(model, y18Ref[0]);
            VerifyNotInScope(model, y18Ref[1]);
            VerifyModelForDeclarationPattern(model, y18Decl);
        }

        [Fact]
        public void ScopeOfPatternVariables_Let_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1(object x1)
    {
        let C1{P1 is var y1} = x1 
            when y1 != null
            else throw (System.Exception)y1;
        System.Console.WriteLine(y1);
    }

    void Test2()
    {
        let C1{P1 is var y2} = y2 
            else throw (System.Exception)y2;
        System.Console.WriteLine(y2);
    }

    void Test3(object x3)
    {
        let var y3 = x3 is var z3
            when z3 != null
            else throw (System.Exception)z3;
        System.Console.WriteLine(y3);
        System.Console.WriteLine(z3);
    }

    void Test4(object x4)
    {
        let var y4 = object.Equals(z4, 
                                   x4 is var z4)
            when z4 != null
            else throw (System.Exception)z4;
        System.Console.WriteLine(y4);
    }

    object Dummy(params object[] a) { return null; }

    void Test5(object x5)
    {
        let var y5 = Dummy(z5)
            when Dummy(z5,
                       x5 is var z5, z5)
            else throw (System.Exception)z5;
        System.Console.WriteLine(y5);
        System.Console.WriteLine(z5);
    }

    void Test6(object x6)
    {
        let System.Guid y6 = x6
            when object.Equals(x6 is var z6, true)
            else throw (System.Exception)z6;
        System.Console.WriteLine(y6);
    }

    void Test7(object x7)
    {
        let var y7 = Dummy(z7)
            when Dummy(z7)
            else throw (System.Exception)Dummy(z7, 
                                               x7 is var z7, 
                                               z7);
        System.Console.WriteLine(y7);
        System.Console.WriteLine(z7);
    }

    void Test8(object x8)
    {
        let var y8 = 11
            else throw (System.Exception)Dummy(x8 is var z8, 
                                               z8);
        System.Console.WriteLine(y8);
    }

    void Test9(object x9)
    {
        let System.Guid y9 = x9
            else let z9 = x9 when z9 is true else System.Console.WriteLine();
        System.Console.WriteLine(y9);
        System.Console.WriteLine(z9);
    }
}

class C1
{
    public object P1 = null;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());

            compilation.VerifyDiagnostics(
    // (83,18): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             else let z9 = x9 when z9 is true else System.Console.WriteLine();
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "let z9 = x9 when z9 is true else System.Console.WriteLine();").WithLocation(83, 18),
    // (12,42): error CS0165: Use of unassigned local variable 'y1'
    //             else throw (System.Exception)y1;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(12, 42),
    // (18,32): error CS0165: Use of unassigned local variable 'y2'
    //         let C1{P1 is var y2} = y2 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(18, 32),
    // (29,34): error CS0103: The name 'z3' does not exist in the current context
    //         System.Console.WriteLine(z3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(29, 34),
    // (34,36): error CS0841: Cannot use local variable 'z4' before it is declared
    //         let var y4 = object.Equals(z4, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z4").WithArguments("z4").WithLocation(34, 36),
    // (45,28): error CS0841: Cannot use local variable 'z5' before it is declared
    //         let var y5 = Dummy(z5)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z5").WithArguments("z5").WithLocation(45, 28),
    // (46,24): error CS0841: Cannot use local variable 'z5' before it is declared
    //             when Dummy(z5,
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z5").WithArguments("z5").WithLocation(46, 24),
    // (50,34): error CS0103: The name 'z5' does not exist in the current context
    //         System.Console.WriteLine(z5);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(50, 34),
    // (57,42): error CS0165: Use of unassigned local variable 'z6'
    //             else throw (System.Exception)z6;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "z6").WithArguments("z6").WithLocation(57, 42),
    // (63,28): error CS0103: The name 'z7' does not exist in the current context
    //         let var y7 = Dummy(z7)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(63, 28),
    // (64,24): error CS0103: The name 'z7' does not exist in the current context
    //             when Dummy(z7)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(64, 24),
    // (65,48): error CS0841: Cannot use local variable 'z7' before it is declared
    //             else throw (System.Exception)Dummy(z7, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "z7").WithArguments("z7").WithLocation(65, 48),
    // (69,34): error CS0103: The name 'z7' does not exist in the current context
    //         System.Console.WriteLine(z7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(69, 34),
    // (85,34): error CS0103: The name 'z9' does not exist in the current context
    //         System.Console.WriteLine(z9);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(85, 34),
    // (84,34): error CS0165: Use of unassigned local variable 'y9'
    //         System.Console.WriteLine(y9);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y9").WithArguments("y9").WithLocation(84, 34)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y1").Single();
            var y1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y1").ToArray();
            Assert.Equal(3, y1Ref.Length);
            foreach (var r in y1Ref)
            {
                VerifyModelForDeclarationPattern(model, y1Decl, r);
            }

            var y2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y2").Single();
            var y2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y2").ToArray();
            Assert.Equal(3, y2Ref.Length);
            VerifyModelForDeclarationPattern(model, y2Decl, y2Ref);

            var y3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y3").Single();
            var y3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y3").Single();
            VerifyModelForDeclarationPattern(model, y3Decl, y3Ref);

            var z3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z3").Single();
            var z3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z3").ToArray();
            Assert.Equal(3, z3Ref.Length);
            VerifyModelForDeclarationPattern(model, z3Decl, z3Ref[0], z3Ref[1]);
            VerifyNotInScope(model, z3Ref[2]);

            var y4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y4").Single();
            var y4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y4").Single();
            VerifyModelForDeclarationPattern(model, y4Decl, y4Ref);

            var let4 = (LetStatementSyntax)y4Decl.Parent;
            Assert.Null(model.GetDeclaredSymbol(let4));
            Assert.Null(model.GetDeclaredSymbol((SyntaxNode)let4));

            var z4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z4").Single();
            var z4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z4").ToArray();
            Assert.Equal(3, z4Ref.Length);
            VerifyModelForDeclarationPattern(model, z4Decl, z4Ref);

            var y5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y5").Single();
            var y5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y5").Single();
            VerifyModelForDeclarationPattern(model, y5Decl, y5Ref);

            var z5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z5").Single();
            var z5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z5").ToArray();
            Assert.Equal(5, z5Ref.Length);
            VerifyModelForDeclarationPattern(model, z5Decl, z5Ref[0], z5Ref[1], z5Ref[2], z5Ref[3]);
            VerifyNotInScope(model, z5Ref[4]);

            var y6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y6").Single();
            var y6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y6").Single();
            VerifyModelForDeclarationPattern(model, y6Decl, y6Ref);

            var z6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z6").Single();
            var z6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z6").Single();
            VerifyModelForDeclarationPattern(model, z6Decl, z6Ref);

            var y7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y7").Single();
            var y7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y7").Single();
            VerifyModelForDeclarationPattern(model, y7Decl, y7Ref);

            var z7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z7").Single();
            var z7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z7").ToArray();
            Assert.Equal(5, z7Ref.Length);
            VerifyNotInScope(model, z7Ref[0]);
            VerifyNotInScope(model, z7Ref[1]);
            VerifyModelForDeclarationPattern(model, z7Decl, z7Ref[2], z7Ref[3]);
            VerifyNotInScope(model, z7Ref[4]);

            var y8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y8").Single();
            var y8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y8").Single();
            VerifyModelForDeclarationPattern(model, y8Decl, y8Ref);

            var z8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "z8").Single();
            var z8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z8").Single();
            VerifyModelForDeclarationPattern(model, z8Decl, z8Ref);

            var y9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "y9").Single();
            var y9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y9").Single();
            VerifyModelForDeclarationPattern(model, y9Decl, y9Ref);

            var z9Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "z9").Single();
            var z9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "z9").ToArray();
            Assert.Equal(2, z9Ref.Length);
            VerifyModelForDeclarationPattern(model, z9Decl, z9Ref[0]);
            VerifyNotInScope(model, z9Ref[1]);
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

    object Test6()
    {
        let x6 = 11;
        Dummy(x6);
        return Dummy(true is var x6, x6);
    }

    object Test7()
    {
        return Dummy(true is var x7, x7);
        let x7 = 11;
        Dummy(x7);
    }

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

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
    // (47,34): error CS0136: A local or parameter named 'x6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x6, x6);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x6").WithArguments("x6").WithLocation(47, 34),
    // (52,34): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x7, x7);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(52, 34),
    // (53,9): warning CS0162: Unreachable code detected
    //         let x7 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "let").WithLocation(53, 9),
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

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);

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

    void Test6()
    {
        let x6 = 11;
        Dummy(x6);
        throw Dummy(true is var x6, x6);
    }

    void Test7()
    {
        throw Dummy(true is var x7, x7);
        let x7 = 11;
        Dummy(x7);
    }

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);

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
    // (47,33): error CS0136: A local or parameter named 'x6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x6, x6);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x6").WithArguments("x6").WithLocation(47, 33),
    // (52,33): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x7, x7);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(52, 33),
    // (53,9): warning CS0162: Unreachable code detected
    //         let x7 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "let").WithLocation(53, 9),
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

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref[1]);

            var x7Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x7").Single();
            var x7Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);

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

    void Test11()
    {
        if (y11 is var x11)
        {   
            let y11 = 12;
            Dummy(y11);
        }
    }

    void Test12()
    {
        if (y12 is var x12)
            var y12 = 12;
    }

    void Test13()
    {
        if (y13 is var x13)
            let y13 = 12;
    }

    void Test14()
    {
        if (Dummy(1 is var x14, 
                  2 is var x14, 
                  x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
    // (110,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(110, 13),
    // (116,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             let y13 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "let y13 = 12;").WithLocation(116, 13),
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
    // (100,13): error CS0103: The name 'y11' does not exist in the current context
    //         if (y11 is var x11)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y11").WithArguments("y11").WithLocation(100, 13),
    // (109,13): error CS0103: The name 'y12' does not exist in the current context
    //         if (y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(109, 13),
    // (115,13): error CS0103: The name 'y13' does not exist in the current context
    //         if (y13 is var x13)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y13").WithArguments("y13").WithLocation(115, 13),
    // (122,28): error CS0128: A local variable named 'x14' is already defined in this scope
    //                   2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(122, 28)
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

            var y11Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "y11").Single();
            var y11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y11").ToArray();
            Assert.Equal(2, y11Ref.Length);
            VerifyNotInScope(model, y11Ref[0]);
            VerifyModelForDeclarationPattern(model, y11Decl, y11Ref[1]);

            var y12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var y13Decl = tree.GetRoot().DescendantNodes().OfType<LetStatementSyntax>().Where(p => p.Identifier.ValueText == "y13").Single();
            var y13Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y13").Single();
            VerifyNotInScope(model, y13Ref);
            VerifyModelForDeclarationPattern(model, y13Decl);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());
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
        public void ScopeOfPatternVariables_ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }


    bool Test1(object o) => let x1 = o;

    bool Test2(object o) => let var x2 = o;

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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions.WithLocalFunctionsFeature());
            compilation.VerifyDiagnostics(
    // (9,33): error CS1002: ; expected
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(9, 33),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (11,33): error CS1002: ; expected
    //     bool Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(11, 33),
    // (11,33): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     bool Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 33),
    // (11,42): error CS0103: The name 'o' does not exist in the current context
    //     bool Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(11, 42),
    // (9,29): error CS0103: The name 'let' does not exist in the current context
    //     bool Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(9, 29),
    // (11,29): error CS0103: The name 'let' does not exist in the current context
    //     bool Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(11, 29),
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
    }
}
