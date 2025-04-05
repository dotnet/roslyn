// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExpressionOptionalAndNamedArgumentsTests : CSharpTestBase
    {
        // PROTOTYPE: Named arguments: Test overloads where the args are in order for one, but not the other.
        // PROTOTYPE: Document breaking changes. Are additional overloads now considered (at least those with default parameters)? Test those cases with language version.

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useIn,
            bool useCompilationReference)
        {
            string refKind = useIn ? "in" : "";
            string sourceA = $$"""
                public static class A
                {
                    public static T GetValue<T>({{refKind}} T t = default) => t;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        Report(A.GetValue<int>());
                        Report(A.GetValue<string>());
                        Report(A.GetValue<S>());
                        Report(Run(() => A.GetValue<int>()));
                        Report(Run(() => A.GetValue<string>()));
                        Report(Run(() => A.GetValue<S>()));
                    }
                    static void Report(object arg)
                    {
                        Console.WriteLine(arg is null ? "null" : arg.ToString());
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            comp = CreateCompilation(
                sourceB,
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (13,26): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => A.GetValue<int>()));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<int>()").WithLocation(13, 26),
                    // (14,26): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => A.GetValue<string>()));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<string>()").WithLocation(14, 26),
                    // (15,26): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => A.GetValue<S>()));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<S>()").WithLocation(15, 26));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    0
                    null
                    S
                    0
                    null
                    S
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_02(bool useIn, bool useCompilationReference)
        {
            string refKind = useIn ? "in" : "";
            string sourceA = $$"""
                public static class A
                {
                    public static int GetIntValue({{refKind}} int i = 10) => i;
                    public static string GetStringValue({{refKind}} string s = "default") => s;
                    public static object GetObjectValue({{refKind}} object o = null) => o;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Report(A.GetIntValue());
                        Report(A.GetStringValue());
                        Report(A.GetObjectValue());
                        Report(Run(() => A.GetIntValue()));
                        Report(Run(() => A.GetStringValue()));
                        Report(Run(() => A.GetObjectValue()));
                    }
                    static void Report(object arg)
                    {
                        Console.WriteLine(arg is null ? "null" : arg.ToString());
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                sourceB,
                references: [refA],
                expectedOutput: """
                    10
                    default
                    null
                    10
                    default
                    null
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_AndParams(bool useIn, bool useCompilationReference)
        {
            string refKind = useIn ? "in" : "";
            string sourceA = $$"""
                public static class A
                {
                    public static T GetValue<T>({{refKind}} T x, {{refKind}} T y = default, params T[] args) => y;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        Report(A.GetValue<int>(1));
                        Report(A.GetValue<int>(2, 3, 4));
                        Report(Run(() => A.GetValue<int>(1)));
                        Report(Run(() => A.GetValue<int>(2, 3, 4)));
                    }
                    static void Report(object arg)
                    {
                        Console.WriteLine(arg is null ? "null" : arg.ToString());
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                sourceB,
                references: [refA],
                expectedOutput: """
                    0
                    3
                    0
                    3
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Constructor(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public class A
                {
                    public readonly string X;
                    public readonly int Y;
                    public A(string x = "default", in int y = 10) { X = x; Y = y; }
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Report(new A("str"));
                        Report(new A());
                        Report(Run(() => new A("str")));
                        Report(Run(() => new A()));
                    }
                    static void Report(A a)
                    {
                        Console.WriteLine((a.X, a.Y));
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            comp = CreateCompilation(
                sourceB,
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,26): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => new A("str")));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, @"new A(""str"")").WithLocation(9, 26),
                    // (10,26): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => new A()));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "new A()").WithLocation(10, 26));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    (str, 10)
                    (default, 10)
                    (str, 10)
                    (default, 10)
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Indexer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public class A
                {
                    public int this[int x, int y = 10] => y;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        var a = new A();
                        Console.WriteLine(a[1, 2]);
                        Console.WriteLine(a[3]);
                        Console.WriteLine(Run(() => a[1, 2]));
                        Console.WriteLine(Run(() => a[3]));
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            comp = CreateCompilation(
                sourceB,
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,37): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Console.WriteLine(Run(() => a[3]));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "a[3]").WithLocation(11, 37));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    2
                    10
                    2
                    10
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Delegate(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public delegate int D(int x, in int y = 10);
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        D d = F<int>;
                        Console.WriteLine(d(1, 2));
                        Console.WriteLine(d(3));
                        Console.WriteLine(Run(() => d(1, 2)));
                        Console.WriteLine(Run(() => d(3)));
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                    static T F<T>(T x, in T y) => y;
                }
                """;
            comp = CreateCompilation(
                sourceB,
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,37): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Console.WriteLine(Run(() => d(3)));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "d(3)").WithLocation(11, 37));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    2
                    10
                    2
                    10
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_CollectionInitializer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useIn)
        {
            string refKind = useIn ? "in" : "";
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<K, V> : IEnumerable
                {
                    private List<KeyValuePair<K, V>> _list = new();
                    public void Add({{refKind}} K k = default, {{refKind}} V v = default) { _list.Add(new(k, v)); }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                """;
            string sourceB = """
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Report(new MyCollection<string, int>() { { "one", 1 }, { "two" } });
                        Report(Run(() => new MyCollection<string, int>() { { "one", 1 }, { "two" } }));
                    }
                    static void Report<K, V>(MyCollection<K, V> c)
                    {
                        foreach (var kvp in c)
                            System.Console.WriteLine("{0}, {1}", kvp.Key, kvp.Value);
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (8,74): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Report(Run(() => new MyCollection<string, int>() { { "one", 1 }, { "two" } }));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, @"{ ""two"" }").WithLocation(8, 74));
            }
            else if (useIn)
            {
                // Expression does not support initializers with ref parameters at runtime.
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        one, 1
                        two, 0
                        one, 1
                        two, 0
                        """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_LocalFunction(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = """
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Expression<Func<int>> e;
                        T Local<T>(T t = default) => t;
                        Local<int>(10);
                        Local<int>();
                        e = () => Local<int>(20);
                        e = () => Local<int>();
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,19): error CS8110: An expression tree may not contain a reference to a local function
                    //         e = () => Local<int>(20);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local<int>(20)").WithLocation(11, 19),
                    // (12,19): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         e = () => Local<int>();
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "Local<int>()").WithLocation(12, 19));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (11,19): error CS8110: An expression tree may not contain a reference to a local function
                    //         e = () => Local<int>(20);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local<int>(20)").WithLocation(11, 19),
                    // (12,19): error CS8110: An expression tree may not contain a reference to a local function
                    //         e = () => Local<int>();
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local<int>()").WithLocation(12, 19));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Decimal(bool preferInterpretation, bool useCompilationReference)
        {
            string value = preferInterpretation ? "true" : "false";
            string sourceA = """
                public static class A
                {
                    public static decimal GetValue(decimal d = 100) => d;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(A.GetValue());
                        Console.WriteLine(Run(() => A.GetValue(200)));
                        Console.WriteLine(Run(() => A.GetValue()));
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                sourceB,
                references: [refA],
                expectedOutput: """
                    100
                    200
                    100
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void DateTime_01(bool preferInterpretation, bool useCompilationReference)
        {
            string value = preferInterpretation ? "true" : "false";
            string sourceA = """
                using System;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                public static class A
                {
                    public static DateTime GetValue([Optional][DateTimeConstant(100)] DateTime value) => value;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(A.GetValue().Ticks);
                        Console.WriteLine(Run(() => A.GetValue(new DateTime(200))).Ticks);
                        Console.WriteLine(Run(() => A.GetValue()).Ticks);
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                sourceB,
                references: [refA],
                expectedOutput: """
                    100
                    200
                    100
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void DateTime_02(bool useCompilationReference)
        {
            string sourceA = """
                using System;
                using System.Runtime.CompilerServices;
                public static class A
                {
                    public static DateTime GetValue([DateTimeConstant(100)] DateTime value) => value;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(A.GetValue().Ticks);
                        Console.WriteLine(Run(() => A.GetValue()).Ticks);
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: [refA]);
            comp.VerifyEmitDiagnostics(
                // (7,29): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(DateTime)'
                //         Console.WriteLine(A.GetValue().Ticks);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(System.DateTime)").WithLocation(7, 29),
                // (8,39): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(DateTime)'
                //         Console.WriteLine(Run(() => A.GetValue()).Ticks);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(System.DateTime)").WithLocation(8, 39));
        }

        [Theory]
        [CombinatorialData]
        public void OptionalAndDefaultParameterValue(bool includeOptional, bool includeDefaultParameterValue, bool useCompilationReference)
        {
            string sourceA = $$"""
                using System.Runtime.InteropServices;
                public static class A
                {
                    public static int GetValue(
                        {{(includeOptional ? "[Optional]" : "")}}
                        {{(includeDefaultParameterValue ? "[DefaultParameterValue(100)]" : "")}}
                        int value) => value;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(A.GetValue());
                        Console.WriteLine(Run(() => A.GetValue(200)));
                        Console.WriteLine(Run(() => A.GetValue()));
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            comp = CreateCompilation(sourceB, references: [refA], options: TestOptions.ReleaseExe);
            if (includeOptional)
            {
                string optionalValue = includeDefaultParameterValue ? "100" : "0";
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: $$"""
                    {{optionalValue}}
                    200
                    {{optionalValue}}
                    """);
                verifier.VerifyDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (7,29): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(int)'
                    //         Console.WriteLine(A.GetValue());
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(int)").WithLocation(7, 29),
                    // (9,39): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(int)'
                    //         Console.WriteLine(Run(() => A.GetValue()));
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(int)").WithLocation(9, 39));
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParams(bool includeDefaultParameterValue, bool useCompilationReference)
        {
            string sourceA = $$"""
                using System.Runtime.InteropServices;
                public static class A
                {
                    public static object[] GetValue(
                        [Optional]
                        {{(includeDefaultParameterValue ? "[DefaultParameterValue(null)]" : "")}}
                        params object[] args) => args;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Report(A.GetValue(1, 2, 3));
                        Report(A.GetValue());
                        Report(Run(() => A.GetValue(1, 2, 3)));
                        Report(Run(() => A.GetValue()));
                    }
                    static void Report(object[] args)
                    {
                        Console.WriteLine(args is null ? "null" : args.Length.ToString());
                    }
                    static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                sourceB,
                references: [refA],
                expectedOutput: """
                    3
                    0
                    3
                    0
                    """);
            verifier.VerifyDiagnostics();
        }
    }
}
