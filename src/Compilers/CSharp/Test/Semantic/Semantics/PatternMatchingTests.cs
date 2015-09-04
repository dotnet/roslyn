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
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6).WithFeature("patterns", "true");

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
    //public static bool operator is(Constant self, out int Value)
    //{
    //    Value = self.Value;
    //    return true;
    //}
}
public class Plus : Expression
{
    public readonly Expression Left, Right;
    public Plus(Expression Left, Expression Right)
    {
        this.Left = Left;
        this.Right = Right;
    }
    //public static bool operator is(Plus self, out Expression Left, out Expression Right)
    //{
    //    Left = self.Left;
    //    Right = self.Right;
    //    return true;
    //}
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
    }
}
