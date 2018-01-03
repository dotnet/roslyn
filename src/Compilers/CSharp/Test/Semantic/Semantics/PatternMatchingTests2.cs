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
    public class PatternMatchingTests2 : PatternMatchingTestBase
    {
        [Fact]
        public void Patterns2_00()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.WriteLine(1 is int {} x ? x : -1);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"1");
        }

        [Fact]
        public void Patterns2_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(3, 4) { Length: 5 } q1 && Check(p, q1));
        Check(false, p is Point(1, 4) { Length: 5 });
        Check(false, p is Point(3, 1) { Length: 5 });
        Check(false, p is Point(3, 4) { Length: 1 });
        Check(true, p is (3, 4) { Length: 5 } q2 && Check(p, q2));
        Check(false, p is (1, 4) { Length: 5 });
        Check(false, p is (3, 1) { Length: 5 });
        Check(false, p is (3, 4) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_02()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(3, 4) { Length: 5 } q1 && Check(p, q1));
        Check(false, p is Point(1, 4) { Length: 5 });
        Check(false, p is Point(3, 1) { Length: 5 });
        Check(false, p is Point(3, 4) { Length: 1 });
        Check(true, p is (3, 4) { Length: 5 } q2 && Check(p, q2));
        Check(false, p is (1, 4) { Length: 5 });
        Check(false, p is (3, 1) { Length: 5 });
        Check(false, p is (3, 4) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public int Length => 5;
}
public static class PointExtensions
{
    public static void Deconstruct(this Point p, out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_DiscardPattern_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(_, _) { Length: _ } q1 && Check(p, q1));
        Check(false, p is Point(1, _) { Length: _ });
        Check(false, p is Point(_, 1) { Length: _ });
        Check(false, p is Point(_, _) { Length: 1 });
        Check(true, p is (_, _) { Length: _ } q2 && Check(p, q2));
        Check(false, p is (1, _) { Length: _ });
        Check(false, p is (_, 1) { Length: _ });
        Check(false, p is (_, _) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact(Skip = "Pattern-based switch with recursive patterns is not yet implemented")]
        public void Patterns2_Switch_Later()
        {
            var source =
@"
class Program
{
    public static void Main()
    {
        Point p = new Point();
        switch (p)
        {
            case Point(3, 4) { Length: 5 }:
                System.Console.WriteLine(true);
                break;
            default:
                System.Console.WriteLine(false);
                break;
        }
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "True");
        }
    }
}
