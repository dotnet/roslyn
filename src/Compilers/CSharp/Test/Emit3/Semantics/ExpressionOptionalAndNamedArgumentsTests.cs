// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExpressionOptionalAndNamedArgumentsTests : CSharpTestBase
    {
        private static string GetUtilities(bool useExpression)
        {
            return useExpression ?
                """
                using System;
                using System.Linq.Expressions;
                public static class Utils
                {
                    public static void Report<T>(Expression<Func<T>> f)
                    {
                        object value = Run(f);
                        Console.WriteLine(value is null ? "null" : value.ToString());
                    }
                    public static T Run<T>(Expression<Func<T>> e)
                    {
                        var f = e.Compile();
                        return f();
                    }
                }
                """ :
                """
                using System;
                public static class Utils
                {
                    public static void Report<T>(Func<T> f)
                    {
                        object value = Run(f);
                        Console.WriteLine(value is null ? "null" : value.ToString());
                    }
                    public static T Run<T>(Func<T> f)
                    {
                        return f();
                    }
                }
                """;
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            [CombinatorialValues("", "in", "ref readonly")] string refKind,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static T GetValue<T>({{refKind}} T t = default) => t;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue<int>());
                        Utils.Report(() => A.GetValue<string>());
                        Utils.Report(() => A.GetValue<S>());
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (8,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetValue<int>());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<int>()").WithLocation(8, 28),
                    // (9,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetValue<string>());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<string>()").WithLocation(9, 28),
                    // (10,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetValue<S>());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetValue<S>()").WithLocation(10, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    0
                    null
                    S
                    """);
                if (useCompilationReference && refKind == "ref readonly")
                {
                    verifier.VerifyDiagnostics(
                        // (3,52): warning CS9200: A default value is specified for 'ref readonly' parameter 't', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                        //     public static T GetValue<T>(ref readonly T t = default) => t;
                        Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "default").WithArguments("t").WithLocation(3, 52));
                }
                else
                {
                    verifier.VerifyDiagnostics();
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_02(
            [CombinatorialValues("", "in", "ref readonly")] string refKind,
            bool useExpression,
            bool useCompilationReference)
        {
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
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetIntValue());
                        Utils.Report(() => A.GetStringValue());
                        Utils.Report(() => A.GetObjectValue());
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                expectedOutput: """
                    10
                    default
                    null
                    """);
            if (useCompilationReference && refKind == "ref readonly")
            {
                verifier.VerifyDiagnostics(
                    // (3,56): warning CS9200: A default value is specified for 'ref readonly' parameter 'i', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                    //     public static int GetIntValue(ref readonly int i = 10) => i;
                    Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "10").WithArguments("i").WithLocation(3, 56),
                    // (4,65): warning CS9200: A default value is specified for 'ref readonly' parameter 's', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                    //     public static string GetStringValue(ref readonly string s = "default") => s;
                    Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, @"""default""").WithArguments("s").WithLocation(4, 65),
                    // (5,65): warning CS9200: A default value is specified for 'ref readonly' parameter 'o', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                    //     public static object GetObjectValue(ref readonly object o = null) => o;
                    Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "null").WithArguments("o").WithLocation(5, 65));
            }
            else
            {
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_AndParams(
            [CombinatorialValues("", "in", "ref readonly")] string refKind,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static T GetValue<T>({{refKind}} T x, {{refKind}} T y = default, params T[] args) => y;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue<int>(1));
                        Utils.Report(() => A.GetValue<int>(2, 3, 4));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                expectedOutput: """
                    0
                    3
                    """);
            if (refKind == "ref readonly")
            {
                if (useCompilationReference)
                {
                    verifier.VerifyDiagnostics(
                        // (3,70): warning CS9200: A default value is specified for 'ref readonly' parameter 'y', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                        //     public static T GetValue<T>(ref readonly T x, ref readonly T y = default, params T[] args) => y;
                        Diagnostic(ErrorCode.WRN_RefReadonlyParameterDefaultValue, "default").WithArguments("y").WithLocation(3, 70),
                        // (8,44): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(1));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "1").WithArguments("1").WithLocation(8, 44),
                        // (9,44): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(2, 3, 4));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "2").WithArguments("1").WithLocation(9, 44),
                        // (9,47): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(2, 3, 4));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "3").WithArguments("2").WithLocation(9, 47));
                }
                else
                {
                    verifier.VerifyDiagnostics(
                        // (8,44): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(1));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "1").WithArguments("1").WithLocation(8, 44),
                        // (9,44): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(2, 3, 4));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "2").WithArguments("1").WithLocation(9, 44),
                        // (9,47): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
                        //         Utils.Report(() => A.GetValue<int>(2, 3, 4));
                        Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "3").WithArguments("2").WithLocation(9, 47));
                }
            }
            else
            {
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Constructor(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public class A
                {
                    public readonly string X;
                    public readonly int Y;
                    public A(string x = "default", in int y = 10) { X = x; Y = y; }
                    public override string ToString() => $"({X}, {Y})";
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => new A("str"));
                        Utils.Report(() => new A());
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => new A("str"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, @"new A(""str"")").WithLocation(5, 28),
                    // (6,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => new A());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "new A()").WithLocation(6, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    (str, 10)
                    (default, 10)
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Indexer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public class A
                {
                    public int this[int x, int y = 10] => y;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = $$"""
                class Program
                {
                    static void Main()
                    {
                        var a = new A();
                        Utils.Report(() => a[1, 2]);
                        Utils.Report(() => a[3]);
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => a[3]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "a[3]").WithLocation(7, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    2
                    10
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_Delegate(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public delegate int D(int x, in int y = 10);
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB = $$"""
                class Program
                {
                    static void Main()
                    {
                        D d = F<int>;
                        Utils.Report(() => d(1, 2));
                        Utils.Report(() => d(3));
                    }
                    static T F<T>(T x, in T y) => y;
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => d(3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "d(3)").WithLocation(7, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    2
                    10
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_CollectionInitializer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useIn)
        {
            string refKind = useIn ? "in" : "";
            string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                using System.Text;
                class MyCollection<K, V> : IEnumerable
                {
                    private List<KeyValuePair<K, V>> _list = new();
                    public void Add({{refKind}} K k = default, {{refKind}} V v = default) { _list.Add(new(k, v)); }
                    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public override string ToString()
                    {
                        var builder = new StringBuilder();
                        foreach (var kvp in this)
                        {
                            if (builder.Length > 0) builder.Append(", ");
                            builder.AppendFormat("({0}, {1})", kvp.Key, kvp.Value);
                        }
                        return builder.ToString();
                    }
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => new MyCollection<string, int>() { { "one", 1 }, { "two" } });
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB, GetUtilities(useExpression)],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,76): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => new MyCollection<string, int>() { { "one", 1 }, { "two" } });
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, @"{ ""two"" }").WithLocation(5, 76));
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
                    expectedOutput: "(one, 1), (two, 0)");
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParameter_LocalFunction(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
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
        public void Decimal(
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = """
                public static class A
                {
                    public static decimal GetValue(decimal d = 100) => d;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue(200));
                        Utils.Report(() => A.GetValue());
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                expectedOutput: """
                    200
                    100
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void DateTime_01(
            bool useExpression,
            bool useCompilationReference)
        {
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
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue(new DateTime(200)).Ticks);
                        Utils.Report(() => A.GetValue().Ticks);
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                expectedOutput: """
                    200
                    100
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void DateTime_02(
            bool useExpression,
            bool useCompilationReference)
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
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue().Ticks);
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA]);
            comp.VerifyEmitDiagnostics(
                // (5,30): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(DateTime)'
                //         Utils.Report(() => A.GetValue().Ticks);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(System.DateTime)").WithLocation(5, 30));
        }

        [Theory]
        [CombinatorialData]
        public void OptionalAndDefaultParameterValue(
            bool includeOptional,
            bool includeDefaultParameterValue,
            bool useExpression,
            bool useCompilationReference)
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
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue(200));
                        Utils.Report(() => A.GetValue());
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                options: TestOptions.ReleaseExe);
            if (includeOptional)
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: $$"""
                        200
                        {{(includeDefaultParameterValue ? "100" : "0")}}
                        """);
                verifier.VerifyDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (6,30): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'A.GetValue(int)'
                    //         Utils.Report(() => A.GetValue());
                    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetValue").WithArguments("value", "A.GetValue(int)").WithLocation(6, 30));
            }
        }

        [Theory]
        [CombinatorialData]
        public void OptionalParams(
            bool includeDefaultParameterValue,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                using System.Runtime.InteropServices;
                public static class A
                {
                    public static string GetValue(
                        [Optional]
                        {{(includeDefaultParameterValue ? "[DefaultParameterValue(null)]" : "")}}
                        params object[] args)
                    {
                        return (args is null) ? "null" : args.Length.ToString();
                    }
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetValue(1, 2, 3));
                        Utils.Report(() => A.GetValue());
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceB, GetUtilities(useExpression)],
                references: [refA],
                expectedOutput: """
                    3
                    0
                    """);
            verifier.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static T GetFirst<T>(T first, T second) => first;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst<int>(1, second:2));
                        Utils.Report(() => A.GetFirst<int>(first: 1, 2));
                        Utils.Report(() => A.GetFirst<int>(first: 1, second:2));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<int>(1, second:2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<int>(1, second:2)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<int>(first: 1, 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<int>(first: 1, 2)").WithLocation(6, 28),
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<int>(first: 1, second:2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<int>(first: 1, second:2)").WithLocation(7, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        1
                        1
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst<int>(second:2, first: 1));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<int>(second:2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<int>(second:2, first: 1)").WithLocation(5, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<int>(second:2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<int>(second:2, first: 1)").WithLocation(5, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB3 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst<int>(2, first: 1));
                        Utils.Report(() => A.GetFirst<int>(second:2, 1));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB3, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,47): error CS1744: Named argument 'first' specifies a parameter for which a positional argument has already been given
                //         Utils.Report(() => A.GetFirst<int>(2, first: 1));
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "first").WithArguments("first").WithLocation(5, 47),
                // (6,44): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetFirst<int>(second:2, 1));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(6, 44));
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static T GetFirst<T>(T first = default, T second = default, T third = default) => first;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst<object>(first: 1, second: 2, third: 3));
                        Utils.Report(() => A.GetFirst<object>(first: 1, 2, 3));
                        Utils.Report(() => A.GetFirst<object>(first: 1, 2));
                        Utils.Report(() => A.GetFirst<object>(first: 1));
                        Utils.Report(() => A.GetFirst<object>(1, second: 2, 3));
                        Utils.Report(() => A.GetFirst<object>(1, second: 2));
                        Utils.Report(() => A.GetFirst<object>(1, 2, third: 3));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<object>(first: 1, second: 2, third: 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<object>(first: 1, second: 2, third: 3)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<object>(first: 1, 2, 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<object>(first: 1, 2, 3)").WithLocation(6, 28),
                    // (7,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(first: 1, 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(first: 1, 2)").WithLocation(7, 28),
                    // (8,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(first: 1)").WithLocation(8, 28),
                    // (9,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<object>(1, second: 2, 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<object>(1, second: 2, 3)").WithLocation(9, 28),
                    // (10,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(1, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(1, second: 2)").WithLocation(10, 28),
                    // (11,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<object>(1, 2, third: 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<object>(1, 2, third: 3)").WithLocation(11, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        1
                        1
                        1
                        1
                        1
                        1
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst<object>(third:3, second: 2, first: 1));
                        Utils.Report(() => A.GetFirst<object>(second:2, first: 1));
                        Utils.Report(() => A.GetFirst<object>(second:2, third: 3));
                        Utils.Report(() => A.GetFirst<object>(second:2));
                        Utils.Report(() => A.GetFirst<object>(third:3, second: 2));
                        Utils.Report(() => A.GetFirst<object>(third:3));
                        Utils.Report(() => A.GetFirst<object>());
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst<object>(third:3, second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst<object>(third:3, second: 2, first: 1)").WithLocation(5, 28),
                    // (6,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(second:2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(second:2, first: 1)").WithLocation(6, 28),
                    // (7,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(second:2, third: 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(second:2, third: 3)").WithLocation(7, 28),
                    // (8,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(second:2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(second:2)").WithLocation(8, 28),
                    // (9,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(third:3, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(third:3, second: 2)").WithLocation(9, 28),
                    // (10,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>(third:3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>(third:3)").WithLocation(10, 28),
                    // (11,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetFirst<object>());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetFirst<object>()").WithLocation(11, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(third:3, second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(third:3, second: 2, first: 1)").WithLocation(5, 28),
                    // (6,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(second:2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(second:2, first: 1)").WithLocation(6, 28),
                    // (7,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(second:2, third: 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(second:2, third: 3)").WithLocation(7, 28),
                    // (8,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(second:2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(second:2)").WithLocation(8, 28),
                    // (9,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(third:3, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(third:3, second: 2)").WithLocation(9, 28),
                    // (10,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst<object>(third:3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst<object>(third:3)").WithLocation(10, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        1
                        null
                        null
                        null
                        null
                        null
                        """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_03(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static (T, int?) GetFirstAndParamsLength<T>(T first, params T[] more) => (first, more?.Length);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirstAndParamsLength(first: 1));
                        Utils.Report(() => A.GetFirstAndParamsLength(first: 1, 2));
                        Utils.Report(() => A.GetFirstAndParamsLength(first: 1, 2, 3));
                        Utils.Report(() => A.GetFirstAndParamsLength(1, more: default));
                        Utils.Report(() => A.GetFirstAndParamsLength(1, more: new[] { 2, 3 }));
                        Utils.Report(() => A.GetFirstAndParamsLength(first: 1, more: default));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(first: 1)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(first: 1, 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(first: 1, 2)").WithLocation(6, 28),
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(first: 1, 2, 3));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(first: 1, 2, 3)").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(1, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(1, more: default)").WithLocation(8, 28),
                    // (9,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(1, more: new[] { 2, 3 }));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(1, more: new[] { 2, 3 })").WithLocation(9, 28),
                    // (10,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(first: 1, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(first: 1, more: default)").WithLocation(10, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (1, 0)
                        (1, 1)
                        (1, 2)
                        (1, )
                        (1, 2)
                        (1, )
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirstAndParamsLength(more: default, first: 1));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirstAndParamsLength(more: default, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirstAndParamsLength(more: default, first: 1)").WithLocation(5, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirstAndParamsLength(more: default, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirstAndParamsLength(more: default, first: 1)").WithLocation(5, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (1, )
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB3 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirstAndParamsLength<int>());
                        Utils.Report(() => A.GetFirstAndParamsLength<int>(more: default));
                        Utils.Report(() => A.GetFirstAndParamsLength<int>(more: default, 1));
                        Utils.Report(() => A.GetFirstAndParamsLength(default, first: 1));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB3, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,30): error CS7036: There is no argument given that corresponds to the required parameter 'first' of 'A.GetFirstAndParamsLength<T>(T, params T[])'
                //         Utils.Report(() => A.GetFirstAndParamsLength<int>());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetFirstAndParamsLength<int>").WithArguments("first", "A.GetFirstAndParamsLength<T>(T, params T[])").WithLocation(5, 30),
                // (6,30): error CS7036: There is no argument given that corresponds to the required parameter 'first' of 'A.GetFirstAndParamsLength<T>(T, params T[])'
                //         Utils.Report(() => A.GetFirstAndParamsLength<int>(more: default));
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "GetFirstAndParamsLength<int>").WithArguments("first", "A.GetFirstAndParamsLength<T>(T, params T[])").WithLocation(6, 30),
                // (7,59): error CS8323: Named argument 'more' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetFirstAndParamsLength<int>(more: default, 1));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "more").WithArguments("more").WithLocation(7, 59),
                // (8,63): error CS1744: Named argument 'first' specifies a parameter for which a positional argument has already been given
                //         Utils.Report(() => A.GetFirstAndParamsLength(default, first: 1));
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "first").WithArguments("first").WithLocation(8, 63));
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_04(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static (T, int?) GetSecondAndParamsLength<T>(T first = default, T second = default, params T[] more) => (second, more?.Length);
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetSecondAndParamsLength<object>());
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2, 3, 4));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2, more: default));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2, more: new[] { 3 }));
                        Utils.Report(() => A.GetSecondAndParamsLength(1));
                        Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2));
                        Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2, 3, 4));
                        Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2, more: default));
                        Utils.Report(() => A.GetSecondAndParamsLength(1, 2, more: default));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength<object>());
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength<object>()").WithLocation(5, 28),
                    // (6,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(first: 1)").WithLocation(6, 28),
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, 2)").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2, 3, 4));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, 2, 3, 4)").WithLocation(8, 28),
                    // (9,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, second: 2)").WithLocation(9, 28),
                    // (10,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, second: 2, more: default)").WithLocation(10, 28),
                    // (11,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, 2, more: new[] { 3 }));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, 2, more: new[] { 3 })").WithLocation(11, 28),
                    // (12,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(1)").WithLocation(12, 28),
                    // (13,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(1, second: 2)").WithLocation(13, 28),
                    // (14,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2, 3, 4));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(1, second: 2, 3, 4)").WithLocation(14, 28),
                    // (15,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(1, second: 2, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(1, second: 2, more: default)").WithLocation(15, 28),
                    // (16,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(1, 2, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(1, 2, more: default)").WithLocation(16, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (, 0)
                        (0, 0)
                        (2, 0)
                        (2, 2)
                        (2, 0)
                        (2, )
                        (2, 1)
                        (0, 0)
                        (2, 0)
                        (2, 2)
                        (2, )
                        (2, )
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2, 3, 4));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default, second: 2));
                        Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, more: default));
                        Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }));
                        Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }, second: 2, first: 1));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, second: 2, 3, 4));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, second: 2, 3, 4)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(first: 1, more: default, second: 2)").WithLocation(6, 28),
                    // (7,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(first: 1, more: default)").WithLocation(7, 28),
                    // (8,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(second: 2)").WithLocation(8, 28),
                    // (9,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(second: 2, first: 1)").WithLocation(9, 28),
                    // (10,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(second: 2, more: default)").WithLocation(10, 28),
                    // (11,28): error CS0854: An expression tree may not contain a call or invocation that uses optional arguments
                    //         Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsOptionalArgument, "A.GetSecondAndParamsLength(more: new[] { 3, 4 })").WithLocation(11, 28),
                    // (12,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }, second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetSecondAndParamsLength(more: new[] { 3, 4 }, second: 2, first: 1)").WithLocation(12, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(first: 1, more: default, second: 2)").WithLocation(6, 28),
                    // (7,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(first: 1, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(first: 1, more: default)").WithLocation(7, 28),
                    // (8,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(second: 2)").WithLocation(8, 28),
                    // (9,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(second: 2, first: 1)").WithLocation(9, 28),
                    // (10,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, more: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(second: 2, more: default)").WithLocation(10, 28),
                    // (11,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(more: new[] { 3, 4 })").WithLocation(11, 28),
                    // (12,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetSecondAndParamsLength(more: new[] { 3, 4 }, second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetSecondAndParamsLength(more: new[] { 3, 4 }, second: 2, first: 1)").WithLocation(12, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (2, 2)
                        (2, )
                        (0, )
                        (2, 0)
                        (2, 0)
                        (2, )
                        (0, 2)
                        (2, 2)
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB3 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, default));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1, default));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1, 3, 4));
                        Utils.Report(() => A.GetSecondAndParamsLength(second: 2, more: default, 1));
                        Utils.Report(() => A.GetSecondAndParamsLength<object>(more: default, default));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB3, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,55): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, default));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(5, 55),
                // (6,55): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1, default));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(6, 55),
                // (7,55): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, first: 1, 3, 4));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(7, 55),
                // (8,55): error CS8323: Named argument 'second' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetSecondAndParamsLength(second: 2, more: default, 1));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "second").WithArguments("second").WithLocation(8, 55),
                // (9,63): error CS8323: Named argument 'more' is used out-of-position but is followed by an unnamed argument
                //         Utils.Report(() => A.GetSecondAndParamsLength<object>(more: default, default));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "more").WithArguments("more").WithLocation(9, 63));
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_OverloadsDifferentOrder(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression,
            bool useCompilationReference)
        {
            string sourceA = $$"""
                public static class A
                {
                    public static int GetFirst(int first, int second) => first;
                    public static string GetFirst(string second, string first) => first ?? "null";
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = AsReference(comp, useCompilationReference);

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst(first: 1, second: 2));
                        Utils.Report(() => A.GetFirst(second: "two", first: "one"));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(first: 1, second: 2));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(first: 1, second: 2)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(second: "two", first: "one"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"A.GetFirst(second: ""two"", first: ""one"")").WithLocation(6, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        one
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst(second: 2, first: 1));
                        Utils.Report(() => A.GetFirst(first: "one", second: "two"));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(second: 2, first: 1)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(first: "one", second: "two"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"A.GetFirst(first: ""one"", second: ""two"")").WithLocation(6, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst(second: 2, first: 1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "A.GetFirst(second: 2, first: 1)").WithLocation(5, 28),
                    // (6,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => A.GetFirst(first: "one", second: "two"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, @"A.GetFirst(first: ""one"", second: ""two"")").WithLocation(6, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        1
                        one
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB3 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst(first: default, default));
                        Utils.Report(() => A.GetFirst(second: default, default));
                        Utils.Report(() => A.GetFirst(default, second: default));
                        Utils.Report(() => A.GetFirst(default, first: default));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB3, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(first: default, default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(first: default, default)").WithLocation(5, 28),
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(second: default, default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(second: default, default)").WithLocation(6, 28),
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(default, second: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(default, second: default)").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => A.GetFirst(default, first: default));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "A.GetFirst(default, first: default)").WithLocation(8, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        0
                        null
                        0
                        null
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB4 = $$"""
                class Program
                {
                    static void Main()
                    {
                        Utils.Report(() => A.GetFirst(first: default, second: default));
                        Utils.Report(() => A.GetFirst(second: default, first: default));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB4, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,30): error CS0121: The call is ambiguous between the following methods or properties: 'A.GetFirst(int, int)' and 'A.GetFirst(string, string)'
                //         Utils.Report(() => A.GetFirst(first: default, second: default));
                Diagnostic(ErrorCode.ERR_AmbigCall, "GetFirst").WithArguments("A.GetFirst(int, int)", "A.GetFirst(string, string)").WithLocation(5, 30),
                // (6,30): error CS0121: The call is ambiguous between the following methods or properties: 'A.GetFirst(int, int)' and 'A.GetFirst(string, string)'
                //         Utils.Report(() => A.GetFirst(second: default, first: default));
                Diagnostic(ErrorCode.ERR_AmbigCall, "GetFirst").WithArguments("A.GetFirst(int, int)", "A.GetFirst(string, string)").WithLocation(6, 30));
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_Constructor(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public class A
                {
                    public readonly string X;
                    public readonly int Y;
                    public A(string x, ref int y) { X = x; Y = y; }
                    public override string ToString() => $"({X}, {Y})";
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        int y = 1;
                        Utils.Report(() => new A(x: "one", ref y));
                        y = 2;
                        Utils.Report(() => new A("two", y: ref y));
                        y = 3;
                        Utils.Report(() => new A(x: "three", y: ref y));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => new A(x: "one", ref y));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"new A(x: ""one"", ref y)").WithLocation(6, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => new A("two", y: ref y));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"new A(""two"", y: ref y)").WithLocation(8, 28),
                    // (10,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => new A(x: "three", y: ref y));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"new A(x: ""three"", y: ref y)").WithLocation(10, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    (one, 1)
                    (two, 2)
                    (three, 3)
                    """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        int y = 1;
                        Utils.Report(() => new A(y: ref y, x: "one"));
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => new A(y: ref y, x: "one"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"new A(y: ref y, x: ""one"")").WithLocation(6, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => new A(y: ref y, x: "one"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, @"new A(y: ref y, x: ""one"")").WithLocation(6, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                    (one, 1)
                    """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_Indexer(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public class A
                {
                    public string this[int x, in string y] => y;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        var a = new A();
                        string y = "one";
                        Utils.Report(() => a[x: 1, in y]);
                        Utils.Report(() => a[2, y: "two"]);
                        y = "three";
                        Utils.Report(() => a[x: 2, y: in y]);
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => a[x: 1, in y]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "a[x: 1, in y]").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => a[2, y: "two"]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"a[2, y: ""two""]").WithLocation(8, 28),
                    // (10,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => a[x: 2, y: in y]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "a[x: 2, y: in y]").WithLocation(10, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        one
                        two
                        three
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        var a = new A();
                        string y = "two";
                        Utils.Report(() => a[y: "one", x: 1]);
                        Utils.Report(() => a[y: in y, x: 2]);
                    }
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => a[y: "one", x: 1]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"a[y: ""one"", x: 1]").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => a[y: in y, x: 2]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "a[y: in y, x: 2]").WithLocation(8, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => a[y: "one", x: 1]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, @"a[y: ""one"", x: 1]").WithLocation(7, 28),
                    // (8,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => a[y: in y, x: 2]);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "a[y: in y, x: 2]").WithLocation(8, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        one
                        two
                        """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_Delegate(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useExpression)
        {
            string sourceA = $$"""
                public delegate (T, U) D<T, U>(T x, U y);
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = $$"""
                class Program
                {
                    static void Main()
                    {
                        D<int, string> d = F;
                        Utils.Report(() => d(x: 1, "one"));
                        Utils.Report(() => d(2, y: "two"));
                        Utils.Report(() => d(x: 3, y: "three"));
                    }
                    static (int, string) F(int i, string s) => (i, s);
                }
                """;
            comp = CreateCompilation(
                [sourceB1, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => d(x: 1, "one"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"d(x: 1, ""one"")").WithLocation(6, 28),
                    // (7,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => d(2, y: "two"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"d(2, y: ""two"")").WithLocation(7, 28),
                    // (8,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => d(x: 3, y: "three"));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"d(x: 3, y: ""three"")").WithLocation(8, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (1, one)
                        (2, two)
                        (3, three)
                        """);
                verifier.VerifyDiagnostics();
            }

            string sourceB2 = $$"""
                class Program
                {
                    static void Main()
                    {
                        D<int, string> d = F;
                        Utils.Report(() => d(y:"one", x:1));
                    }
                    static (int, string) F(int i, string s) => (i, s);
                }
                """;
            comp = CreateCompilation(
                [sourceB2, GetUtilities(useExpression)],
                references: [refA],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe);
            if (languageVersion == LanguageVersion.CSharp13 && useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS0853: An expression tree may not contain a named argument specification
                    //         Utils.Report(() => d(y:"one", x:1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, @"d(y:""one"", x:1)").WithLocation(6, 28));
            }
            else if (useExpression)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,28): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         Utils.Report(() => d(y:"one", x:1));
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, @"d(y:""one"", x:1)").WithLocation(6, 28));
            }
            else
            {
                var verifier = CompileAndVerify(
                    comp,
                    expectedOutput: """
                        (1, one)
                        """);
                verifier.VerifyDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void NamedArgument_LocalFunction(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = """
                using System;
                using System.Linq.Expressions;
                class Program
                {
                    static void Main()
                    {
                        Expression<Func<int>> e;
                        T GetFirst<T>(T first, T second) => first;
                        GetFirst(first: 1, 2);
                        GetFirst(1, second: 2);
                        GetFirst(second: 2, first: 1);
                        e = () => GetFirst(first: 1, 2);
                        e = () => GetFirst(1, second: 2);
                        e = () => GetFirst(second: 2, first: 1);
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (12,19): error CS0853: An expression tree may not contain a named argument specification
                    //         e = () => GetFirst(first: 1, 2);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "GetFirst(first: 1, 2)").WithLocation(12, 19),
                    // (13,19): error CS0853: An expression tree may not contain a named argument specification
                    //         e = () => GetFirst(1, second: 2);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "GetFirst(1, second: 2)").WithLocation(13, 19),
                    // (14,19): error CS0853: An expression tree may not contain a named argument specification
                    //         e = () => GetFirst(second: 2, first: 1);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgument, "GetFirst(second: 2, first: 1)").WithLocation(14, 19));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (12,19): error CS8110: An expression tree may not contain a reference to a local function
                    //         e = () => GetFirst(first: 1, 2);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "GetFirst(first: 1, 2)").WithLocation(12, 19),
                    // (13,19): error CS8110: An expression tree may not contain a reference to a local function
                    //         e = () => GetFirst(1, second: 2);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "GetFirst(1, second: 2)").WithLocation(13, 19),
                    // (14,19): error CS9307: An expression tree may not contain a named argument specification out of position
                    //         e = () => GetFirst(second: 2, first: 1);
                    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition, "GetFirst(second: 2, first: 1)").WithLocation(14, 19));
            }
        }
    }
}
