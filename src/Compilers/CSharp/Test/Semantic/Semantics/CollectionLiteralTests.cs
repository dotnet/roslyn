// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralTests : CSharpTestBase
    {
        private const string s_collectionExtensions = """
            using System;
            using System.Collections;
            using System.Linq;
            using System.Text;
            static partial class CollectionExtensions
            {
                private static void Append(StringBuilder builder, bool isFirst, object value)
                {
                    if (!isFirst) builder.Append(", ");
                    if (value is IEnumerable e && value is not string)
                    {
                        AppendCollection(builder, e);
                    }
                    else
                    {
                        builder.Append(value is null ? "null" : value.ToString());
                    }
                }
                private static void AppendCollection(StringBuilder builder, IEnumerable e)
                {
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var i in e)
                    {
                        Append(builder, isFirst, i);
                        isFirst = false;
                    }
                    builder.Append("]");
                }
                internal static void Report(this object o, bool includeType = false)
                {
                    var builder = new StringBuilder();
                    Append(builder, isFirst: true, o);
                    if (includeType) Console.Write(GetTypeName(o.GetType()));
                    Console.Write(builder.ToString());
                    Console.Write(", ");
                }
                private static string GetTypeName(Type type)
                {
                    if (type.IsArray)
                    {
                        return GetTypeName(type.GetElementType()) + "[]";
                    }
                    string typeName = type.Name;
                    int index = typeName.LastIndexOf('`');
                    if (index >= 0)
                    {
                        typeName = typeName.Substring(0, index);
                    }
                    typeName = Concat(type.Namespace, typeName);
                    if (!type.IsGenericType)
                    {
                        return typeName;
                    }
                    return $"{typeName}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>";
                }
                private static string Concat(string container, string name)
                {
                    return string.IsNullOrEmpty(container) ? name : container + "." + name;
                }
            }
            """;
        private const string s_collectionExtensionsWithSpan = s_collectionExtensions +
            """
            static partial class CollectionExtensions
            {
                internal static void Report<T>(this in Span<T> s)
                {
                    Report((ReadOnlySpan<T>)s);
                }
                internal static void Report<T>(this in ReadOnlySpan<T> s)
                {
                    var builder = new StringBuilder();
                    builder.Append("[");
                    bool isFirst = true;
                    foreach (var i in s)
                    {
                        Append(builder, isFirst, i);
                        isFirst = false;
                    }
                    builder.Append("]");
                    Console.Write(builder.ToString());
                    Console.Write(", ");
                }
            }
            """;

        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Preview)]
        public void LanguageVersionDiagnostics(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        object[] x = [];
                        List<object> y = [1, 2, 3];
                        List<object[]> z = [[]];
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp11)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,22): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         object[] x = [];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(6, 22),
                    // (7,26): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         List<object> y = [1, 2, 3];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(7, 26),
                    // (8,28): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         List<object[]> z = [[]];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(8, 28),
                    // (8,29): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         List<object[]> z = [[]];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(8, 29));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Fact]
        public void NaturalType_01()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [];
                        dynamic y = [];
                        var z = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report ERR_ImplicitlyTypedCollectionLiteralNoBestType for x, y, z.
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9500: Cannot initialize type 'dynamic' with a collection literal because the type is not constructible.
                //         dynamic y = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var z = [];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[]").WithLocation(7, 17));
        }

        [Fact]
        public void NaturalType_02()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [1];
                        dynamic y = [2];
                        var z = [3];
                        x.Report(includeType: true);
                        ((object)y).Report(includeType: true);
                        z.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, references: new[] { CSharpRef }, expectedOutput:
                "System.Collections.Generic.List<System.Int32>[1], System.Collections.Generic.List<System.Int32>[2], System.Collections.Generic.List<System.Int32>[3], ");

            // PROTOTYPE: Use semantic model to verify types and converted types. See SemanticModel() test below.
        }

        [Fact]
        public void NaturalType_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [1, ""];
                        dynamic y = [2, ""];
                        var z = [3, ""];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report ERR_ImplicitlyTypedCollectionLiteralNoBestType for x and y.
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object x = [1, ""];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, @"[1, """"]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9500: Cannot initialize type 'dynamic' with a collection literal because the type is not constructible.
                //         dynamic y = [2, ""];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, @"[2, """"]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var z = [3, ""];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, @"[3, """"]").WithLocation(7, 17));
        }

        [Fact]
        public void NaturalType_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object x = [null];
                        dynamic y = [null];
                        var z = [null];
                        int?[] w = [null];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report ERR_ImplicitlyTypedCollectionLiteralNoBestType for x and y.
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object x = [null];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[null]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9500: Cannot initialize type 'dynamic' with a collection literal because the type is not constructible.
                //         dynamic y = [null];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[null]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var z = [null];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[null]").WithLocation(7, 17));
        }

        [Fact]
        public void NaturalType_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var x = [1, 2, null];
                        object y = [1, 2, null];
                        dynamic z = [1, 2, null];
                        int?[] w = [1, 2, null];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,24): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //         var x = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "null").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(5, 24),
                // (5,24): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var x = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(5, 24),
                // (6,27): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //         object y = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "null").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(6, 27),
                // (6,27): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         object y = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(6, 27),
                // (7,28): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //         dynamic z = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "null").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(7, 28),
                // (7,28): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         dynamic z = [1, 2, null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(7, 28));
        }

        [Fact]
        public void NaturalType_06()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[] x = [[]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object[] x = [[]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 23));
        }

        [Fact]
        public void NaturalType_07()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[] y = [[2]];
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "System.Object[][[2]], ");
        }

        [Fact]
        public void NaturalType_08()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        var z = [[3]];
                        z.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>[[3]], ");
        }

        [Fact]
        public void NaturalType_09()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F([]);
                        F([[]]);
                    }
                    static T F<T>(T t) => t;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(5, 9),
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([[]]);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(6, 9));
        }

        [Fact]
        public void NaturalType_10()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F([1, 2]).Report();
                        F([[3, 4]]).Report();
                    }
                    static T F<T>(T t) => t;
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], [[3, 4]], ");
        }

        [Fact]
        public void NaturalType_11()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var d1 = () => [];
                        Func<int[]> d2 = () => [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,18): error CS8917: The delegate type could not be inferred.
                //         var d1 = () => [];
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "() => []").WithLocation(6, 18));
        }

        [Fact]
        public void NaturalType_12()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        IEnumerable x = [];
                        IEnumerable<int> y = [];
                        IList<object> z = [];
                        IDictionary<string, int> w = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,25): error CS0144: Cannot create an instance of the abstract type or interface 'IEnumerable'
                //         IEnumerable x = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("System.Collections.IEnumerable").WithLocation(7, 25),
                // (7,25): error CS9500: Cannot initialize type 'IEnumerable' with a collection literal because the type is not constructible.
                //         IEnumerable x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("System.Collections.IEnumerable").WithLocation(7, 25),
                // (8,30): error CS0144: Cannot create an instance of the abstract type or interface 'IEnumerable<int>'
                //         IEnumerable<int> y = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("System.Collections.Generic.IEnumerable<int>").WithLocation(8, 30),
                // (9,27): error CS0144: Cannot create an instance of the abstract type or interface 'IList<object>'
                //         IList<object> z = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("System.Collections.Generic.IList<object>").WithLocation(9, 27),
                // (10,38): error CS0144: Cannot create an instance of the abstract type or interface 'IDictionary<string, int>'
                //         IDictionary<string, int> w = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("System.Collections.Generic.IDictionary<string, int>").WithLocation(10, 38));
        }

        [Fact]
        public void NaturalType_13()
        {
            string source = """
                #nullable enable
                class Program
                {
                    static void Main()
                    {
                        var x = [null, ""];
                        var y = [new object(), (string?)null];
                        var z = [new object(), ""];
                        x[0] = null;
                        y[0] = null;
                        z[0] = null; // 1
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report warning for // 1.
            comp.VerifyEmitDiagnostics();
        }

        // PROTOTYPE: Test common type with inferred function types.
        // PROTOTYPE: Test common type with one or more error types.

        // PROTOTYPE: Test conversions of each element. Test diagnostics are only reported when not target-typed to constructible type.
        // PROTOTYPE: Test use-site error on best common type. Test diagnostics are only reported when not target-typed to constructible type.
        // PROTOTYPE: Test collections where the element types differ by dynamic/object, tuple element names, nint/IntPtr, nullable.

        // PROTOTYPE: Test natural type where List<T>.Add() method is missing or has unexpected parameter type.

        // PROTOTYPE: Test member access: [1, 2].Length, [3, 4]?.Length
        // PROTOTYPE: Test invocaton: [1, 2]()
        // PROTOTYPE: Test as argument to overloads, where overloads differ by collection type.
        // PROTOTYPE: Test as method type inference argument.
        // PROTOTYPE: Test in RHS of tuple conversion. For instance, (var x, var y) = ([1, 2], []);
        // PROTOTYPE: Test all cases where we call BindToNaturalType(expr). For instance, BindThrowExpression().

        [Fact]
        public void NaturalType_14()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F(false);
                    }
                    static void F(bool b)
                    {
                        var x = b ? [1] : [];
                        var y = b? [] : [2];
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput:
                "System.Collections.Generic.List<System.Int32>[], System.Collections.Generic.List<System.Int32>[2], ");

            // PROTOTYPE: Test the same with: switch expression; lambda returns.
        }

        [Fact]
        public void NaturalType_15()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F(false, 3);
                    }
                    static void F(bool b, object o)
                    {
                        var x = b ? [1] : [o];
                        var y = b? [o] : [2];
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS0172: Type of conditional expression cannot be determined because 'List<int>' and 'List<object>' implicitly convert to one another
                //         var x = b ? [1] : [o];
                Diagnostic(ErrorCode.ERR_AmbigQM, "b ? [1] : [o]").WithArguments("System.Collections.Generic.List<int>", "System.Collections.Generic.List<object>").WithLocation(9, 17),
                // (10,17): error CS0172: Type of conditional expression cannot be determined because 'List<object>' and 'List<int>' implicitly convert to one another
                //         var y = b? [o] : [2];
                Diagnostic(ErrorCode.ERR_AmbigQM, "b? [o] : [2]").WithArguments("System.Collections.Generic.List<object>", "System.Collections.Generic.List<int>").WithLocation(10, 17));
        }

        [Fact]
        public void NaturalType_16()
        {
            string source = """
                class Program
                {
                    static void F(bool b)
                    {
                        var x = b ? [1] : [null];
                        var y = b? [null] : [2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report "error CS0173: Type of conditional expression cannot be determined because ..."
            // for x and y. Compare with equivalent test with switch expressions.
            comp.VerifyEmitDiagnostics(
                // (5,28): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //         var x = b ? [1] : [null];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "null").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(5, 28),
                // (5,28): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var x = b ? [1] : [null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(5, 28),
                // (6,21): error CS1950: The best overloaded Add method 'List<int>.Add(int)' for the collection initializer has some invalid arguments
                //         var y = b? [null] : [2];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "null").WithArguments("System.Collections.Generic.List<int>.Add(int)").WithLocation(6, 21),
                // (6,21): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var y = b? [null] : [2];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(6, 21));
        }

        [Fact]
        public void NaturalType_17()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        F(false);
                    }
                    static void F(bool b)
                    {
                        var x = b ? ["1"] : [null];
                        var y = b? [null] : ["2"];
                        x.Report(includeType: true);
                        y.Report(includeType: true);
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput:
                "System.Collections.Generic.List<System.String>[null], System.Collections.Generic.List<System.String>[2], ");
        }

        [Fact]
        public void NaturalType_18()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int n = [].Count;
                        var v = [][0];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         int n = [].Count;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[]").WithLocation(5, 17),
                // (6,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var v = [][0];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[]").WithLocation(6, 17));
        }

        [Fact]
        public void NaturalType_19()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        int n = [1, 2, 3].Count;
                        var v = [1, 2, 3][0];
                        Console.WriteLine($"{n}, {v}");
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "3, 1");
        }

        // PROTOTYPE: Test natural type with missing List<T> type.
        // PROTOTYPE: Test natural type with missing List<T> constructor.
        // PROTOTYPE: Test natural type with missing List<T> type use site error (perhaps missing interface).

        [Fact]
        public void Array_01()
        {
            string source = """
                class Program
                {
                    static int[] Create1() => [];
                    static object[] Create2() => [1, 2];
                    static int[] Create3() => [3, 4, 5];
                    static long?[] Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "int"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       25 (0x19)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  box        "int"
                  IL_000e:  stelem.ref
                  IL_000f:  dup
                  IL_0010:  ldc.i4.1
                  IL_0011:  ldc.i4.2
                  IL_0012:  box        "int"
                  IL_0017:  stelem.ref
                  IL_0018:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       18 (0x12)
                  .maxstack  3
                  IL_0000:  ldc.i4.3
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D4"
                  IL_000c:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                  IL_0011:  ret
                }
                """);
            verifier.VerifyIL("Program.Create4", """
                {
                  // Code size       21 (0x15)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "long?"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.1
                  IL_0008:  ldc.i4.7
                  IL_0009:  conv.i8
                  IL_000a:  newobj     "long?..ctor(long)"
                  IL_000f:  stelem     "long?"
                  IL_0014:  ret
                }
                """);
        }

        [Fact]
        public void Array_02()
        {
            string source = """
                using System;
                class Program
                {
                    static int[][] Create1() => [];
                    static object[][] Create2() => [[]];
                    static object[][] Create3() => [[1], [2, 3]];
                    static void Main()
                    {
                        Report(Create1());
                        Report(Create2());
                        Report(Create3());
                    }
                    static void Report<T>(T[][] a)
                    {
                        Console.Write("Length={0}, ", a.Length);
                        foreach (var x in a)
                        {
                            Console.Write("Length={0}, ", x.Length);
                            foreach (var y in x)
                                Console.Write("{0}, ", y);
                        }
                        Console.WriteLine();
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                Length=0, 
                Length=1, Length=0, 
                Length=2, Length=1, 1, Length=2, 2, 3, 
                """);
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "int[]"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       16 (0x10)
                  .maxstack  4
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "object[]"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.0
                  IL_0009:  newarr     "object"
                  IL_000e:  stelem.ref
                  IL_000f:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       52 (0x34)
                  .maxstack  7
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object[]"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  newarr     "object"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.0
                  IL_0010:  ldc.i4.1
                  IL_0011:  box        "int"
                  IL_0016:  stelem.ref
                  IL_0017:  stelem.ref
                  IL_0018:  dup
                  IL_0019:  ldc.i4.1
                  IL_001a:  ldc.i4.2
                  IL_001b:  newarr     "object"
                  IL_0020:  dup
                  IL_0021:  ldc.i4.0
                  IL_0022:  ldc.i4.2
                  IL_0023:  box        "int"
                  IL_0028:  stelem.ref
                  IL_0029:  dup
                  IL_002a:  ldc.i4.1
                  IL_002b:  ldc.i4.3
                  IL_002c:  box        "int"
                  IL_0031:  stelem.ref
                  IL_0032:  stelem.ref
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void Array_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object o;
                        o = (int[])[];
                        o.Report();
                        o = (long?[])[null, 2];
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [null, 2], ");
        }

        [Fact]
        public void Array_04()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[,] x = [];
                        int[,] y = [null, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9500: Cannot initialize type 'object[*,*]' with a collection literal because the type is not constructible.
                //         object[,] x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("object[*,*]").WithLocation(5, 23),
                // (6,20): error CS9500: Cannot initialize type 'int[*,*]' with a collection literal because the type is not constructible.
                //         int[,] y = [null, 2];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[null, 2]").WithArguments("int[*,*]").WithLocation(6, 20),
                // (6,21): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int[,] y = [null, 2];
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 21));
        }

        [Fact]
        public void Array_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int[,] z = [[1, 2], [3, 4]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'int[*,*]' with a collection literal because the type is not constructible.
                //         int[,] z = [[1, 2], [3, 4]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[[1, 2], [3, 4]]").WithArguments("int[*,*]").WithLocation(5, 20),
                // (5,21): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'int'
                //         int[,] z = [[1, 2], [3, 4]];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "int").WithLocation(5, 21),
                // (5,29): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'int'
                //         int[,] z = [[1, 2], [3, 4]];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[3, 4]").WithArguments("System.Collections.Generic.List<int>", "int").WithLocation(5, 29));
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Span_01(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static {{spanType}}<int> Create1() => [];
                    static {{spanType}}<object> Create2() => [1, 2];
                    static {{spanType}}<int> Create3() => [3, 4, 5];
                    static {{spanType}}<long?> Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensionsWithSpan }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
            verifier.VerifyIL("Program.Create1", $$"""
                {
                  // Code size       12 (0xc)
                  .maxstack  1
                  IL_0000:  ldc.i4.0
                  IL_0001:  newarr     "int"
                  IL_0006:  newobj     "System.{{spanType}}<int>..ctor(int[])"
                  IL_000b:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", $$"""
                {
                  // Code size       30 (0x1e)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "object"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  box        "int"
                  IL_000e:  stelem.ref
                  IL_000f:  dup
                  IL_0010:  ldc.i4.1
                  IL_0011:  ldc.i4.2
                  IL_0012:  box        "int"
                  IL_0017:  stelem.ref
                  IL_0018:  newobj     "System.{{spanType}}<object>..ctor(object[])"
                  IL_001d:  ret
                }
                """);
            if (useReadOnlySpan)
            {
                verifier.VerifyIL("Program.Create3", """
                    {
                      // Code size       11 (0xb)
                      .maxstack  1
                      IL_0000:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12_Align=4 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D44"
                      IL_0005:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
                      IL_000a:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("Program.Create3", """
                    {
                      // Code size       23 (0x17)
                      .maxstack  3
                      IL_0000:  ldc.i4.3
                      IL_0001:  newarr     "int"
                      IL_0006:  dup
                      IL_0007:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.CE99AE045C8B2A2A8A58FD1A2120956E74E90322EEF45F7DFE1CA73EEFE655D4"
                      IL_000c:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                      IL_0011:  newobj     "System.Span<int>..ctor(int[])"
                      IL_0016:  ret
                    }
                    """);
            }
            verifier.VerifyIL("Program.Create4", $$"""
                {
                  // Code size       26 (0x1a)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "long?"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.1
                  IL_0008:  ldc.i4.7
                  IL_0009:  conv.i8
                  IL_000a:  newobj     "long?..ctor(long)"
                  IL_000f:  stelem     "long?"
                  IL_0014:  newobj     "System.{{spanType}}<long?>..ctor(long?[])"
                  IL_0019:  ret
                }
                """);
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Span_02(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static void Main()
                    {
                        {{spanType}}<string> x = [];
                        {{spanType}}<int> y = [1, 2, 3];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensionsWithSpan }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2, 3], ");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Span_03(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static void Main()
                    {
                        var x = ({{spanType}}<string>)[];
                        var y = ({{spanType}}<int>)[1, 2, 3];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensionsWithSpan }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2, 3], ");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Span_04(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static ref readonly {{spanType}}<int> F1()
                    {
                        return ref F2<int>([]);
                    }
                    static ref readonly {{spanType}}<T> F2<T>(in {{spanType}}<T> s)
                    {
                        return ref s;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (6,20): error CS8347: Cannot use a result of 'Program.F2<int>(in Span<int>)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2<int>([])").WithArguments($"Program.F2<int>(in System.{spanType}<int>)", "s").WithLocation(6, 20),
                // (6,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "[]").WithLocation(6, 28));
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Span_05(bool useReadOnlySpan)
        {
            string spanType = useReadOnlySpan ? "ReadOnlySpan" : "Span";
            string source = $$"""
                using System;
                class Program
                {
                    static ref readonly {{spanType}}<int> F1()
                    {
                        return ref F2<int>([]);
                    }
                    static ref readonly {{spanType}}<T> F2<T>(scoped in {{spanType}}<T> s)
                    {
                        throw null;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Span_MissingConstructor()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Span<string> x = [];
                        ReadOnlySpan<int> y = [1, 2, 3];
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //         Span<string> x = [];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Span`1", ".ctor").WithLocation(6, 26));

            comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array);
            comp.VerifyEmitDiagnostics(
                // (7,31): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1..ctor'
                //         ReadOnlySpan<int> y = [1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[1, 2, 3]").WithArguments("System.ReadOnlySpan`1", ".ctor").WithLocation(7, 31));
        }

        [Fact]
        public void InterfaceType()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Runtime.InteropServices;

                [ComImport]
                [Guid("1FC6664D-C61E-4131-81CD-A3EE0DD6098F")]
                [CoClass(typeof(C))]
                interface I : IEnumerable
                {
                    void Add(int i);
                }

                class C : I
                {
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    void I.Add(int i) { }
                }

                class Program
                {
                    static void Main()
                    {
                        I i;
                        i = new() { };
                        i = new() { 1, 2 };
                        i = [];
                        i = [3, 4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (26,13): error CS0144: Cannot create an instance of the abstract type or interface 'I'
                //         i = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("I").WithLocation(26, 13),
                // (27,13): error CS0144: Cannot create an instance of the abstract type or interface 'I'
                //         i = [3, 4];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[3, 4]").WithArguments("I").WithLocation(27, 13));
        }

        [Fact]
        public void EnumType_01()
        {
            string source = """
                enum E { }
                class Program
                {
                    static void Main()
                    {
                        E e;
                        e = [];
                        e = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9500: Cannot initialize type 'E' with a collection literal because the type is not constructible.
                //         e = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("E").WithLocation(7, 13),
                // (8,13): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'E'
                //         e = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "E").WithLocation(8, 13));
        }

        [Fact]
        public void EnumType_02()
        {
            string sourceA = """
                using System.Collections;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct Enum : IEnumerable { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class List<T> : IEnumerable { }
                }
                """;
            string sourceB = """
                enum E { }
                class Program
                {
                    static void Main()
                    {
                        E e;
                        e = [];
                        e = [1, 2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (8,14): error CS1061: 'E' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'E' could be found (are you missing a using directive or an assembly reference?)
                //         e = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("E", "Add").WithLocation(8, 14),
                // (8,17): error CS1061: 'E' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'E' could be found (are you missing a using directive or an assembly reference?)
                //         e = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "2").WithArguments("E", "Add").WithLocation(8, 17));
        }

        [Fact]
        public void DelegateType_01()
        {
            string source = """
                delegate void D();
                class Program
                {
                    static void Main()
                    {
                        D d;
                        d = [];
                        d = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9500: Cannot initialize type 'D' with a collection literal because the type is not constructible.
                //         d = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("D").WithLocation(7, 13),
                // (7,13): error CS7036: There is no argument given that corresponds to the required parameter 'object' of 'D.D(object, IntPtr)'
                //         d = [];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[]").WithArguments("object", "D.D(object, System.IntPtr)").WithLocation(7, 13),
                // (8,13): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'D'
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "D").WithLocation(8, 13));
        }

        [Fact]
        public void DelegateType_02()
        {
            string sourceA = """
                using System.Collections;
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public struct IntPtr { }
                    public abstract class Delegate : IEnumerable { }
                    public abstract class MulticastDelegate : Delegate { }
                }
                namespace System.Collections
                {
                    public interface IEnumerable { }
                }
                namespace System.Collections.Generic
                {
                    public class List<T> : IEnumerable { }
                }
                """;
            string sourceB = """
                delegate void D();
                class Program
                {
                    static void Main()
                    {
                        D d;
                        d = [];
                        d = [1, 2];
                    }
                }
                """;
            var comp = CreateEmptyCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());
            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (7,13): error CS7036: There is no argument given that corresponds to the required parameter 'object' of 'D.D(object, IntPtr)'
                //         d = [];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[]").WithArguments("object", "D.D(object, System.IntPtr)").WithLocation(7, 13),
                // (8,13): error CS7036: There is no argument given that corresponds to the required parameter 'object' of 'D.D(object, IntPtr)'
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[1, 2]").WithArguments("object", "D.D(object, System.IntPtr)").WithLocation(8, 13),
                // (8,14): error CS1061: 'D' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'D' could be found (are you missing a using directive or an assembly reference?)
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("D", "Add").WithLocation(8, 14),
                // (8,17): error CS1061: 'D' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'D' could be found (are you missing a using directive or an assembly reference?)
                //         d = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "2").WithArguments("D", "Add").WithLocation(8, 17));
        }

        [Fact]
        public void PointerType_01()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        int* x = [];
                        int* y = [1, 2];
                        var z = (int*)[3];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,18): error CS9500: Cannot initialize type 'int*' with a collection literal because the type is not constructible.
                //         int* x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("int*").WithLocation(5, 18),
                // (6,18): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'int*'
                //         int* y = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "int*").WithLocation(6, 18),
                // (7,23): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'int*'
                //         var z = (int*)[3];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[3]").WithArguments("System.Collections.Generic.List<int>", "int*").WithLocation(7, 23));
        }

        [Fact]
        public void PointerType_02()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        delegate*<void> x = [];
                        delegate*<void> y = [1, 2];
                        var z = (delegate*<void>)[3];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (5,29): error CS9500: Cannot initialize type 'delegate*<void>' with a collection literal because the type is not constructible.
                //         delegate*<void> x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("delegate*<void>").WithLocation(5, 29),
                // (6,29): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'delegate*<void>'
                //         delegate*<void> y = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "delegate*<void>").WithLocation(6, 29),
                // (7,34): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'delegate*<void>'
                //         var z = (delegate*<void>)[3];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[3]").WithArguments("System.Collections.Generic.List<int>", "delegate*<void>").WithLocation(7, 34));
        }

        [Fact]
        public void PointerType_03()
        {
            string source = """
                class Program
                {
                    unsafe static void Main()
                    {
                        void* p = null;
                        delegate*<void> d = null;
                        var x = [p];
                        var y = [d];
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseExe);
            // PROTOTYPE: Should report error for List<void*> and List<delegate*<void>>.
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void PointerType_04()
        {
            string source = """
                using System;
                class Program
                {
                    unsafe static void Main()
                    {
                        void*[] a = [null, (void*)2];
                        foreach (void* p in a)
                            Console.Write("{0}, ", (nint)p);
                    }
                }
                """;
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "0, 2, ");
        }

        [Fact]
        public void PointerType_05()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    private List<nint> _list = new List<nint>();
                    unsafe public void Add(void* p) { _list.Add((nint)p); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    unsafe static void Main()
                    {
                        void* p = (void*)2;
                        C c = [null, p];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, options: TestOptions.UnsafeReleaseExe, verify: Verification.Skipped, expectedOutput: "[0, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static List<int> Create1() => [];
                    static List<object> Create2() => [1, 2];
                    static List<int> Create3() => [3, 4, 5];
                    static List<long?> Create4() => [null, 7];
                    static void Main()
                    {
                        Create1().Report();
                        Create2().Report();
                        Create3().Report();
                        Create4().Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
            verifier.VerifyIL("Program.Create1", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create2", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  3
                  IL_0000:  newobj     "System.Collections.Generic.List<object>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  box        "int"
                  IL_000c:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_0011:  dup
                  IL_0012:  ldc.i4.2
                  IL_0013:  box        "int"
                  IL_0018:  callvirt   "void System.Collections.Generic.List<object>.Add(object)"
                  IL_001d:  ret
                }
                """);
            verifier.VerifyIL("Program.Create3", """
                {
                  // Code size       27 (0x1b)
                  .maxstack  3
                  IL_0000:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.3
                  IL_0007:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_000c:  dup
                  IL_000d:  ldc.i4.4
                  IL_000e:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_0013:  dup
                  IL_0014:  ldc.i4.5
                  IL_0015:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_001a:  ret
                }
                """);
            verifier.VerifyIL("Program.Create4", """
                {
                  // Code size       34 (0x22)
                  .maxstack  3
                  .locals init (long? V_0)
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  initobj    "long?"
                  IL_000e:  ldloc.0
                  IL_000f:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0014:  dup
                  IL_0015:  ldc.i4.7
                  IL_0016:  conv.i8
                  IL_0017:  newobj     "long?..ctor(long)"
                  IL_001c:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_02()
        {
            string source = """
                S s;
                s = [];
                s = [1, 2];
                struct S { }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9500: Cannot initialize type 'S' with a collection literal because the type is not constructible.
                // s = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(2, 5),
                // (3,5): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(3, 5));
        }

        [Fact]
        public void CollectionInitializerType_03()
        {
            string source = """
                using System.Collections;
                S s;
                s = [];
                struct S : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            CompileAndVerify(source, expectedOutput: "");

            source = """
                using System.Collections;
                S s;
                s = [1, 2];
                struct S : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,6): error CS1061: 'S' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'S' could be found (are you missing a using directive or an assembly reference?)
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "1").WithArguments("S", "Add").WithLocation(3, 6),
                // (3,9): error CS1061: 'S' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'S' could be found (are you missing a using directive or an assembly reference?)
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "2").WithArguments("S", "Add").WithLocation(3, 9));
        }

        [Fact]
        public void CollectionInitializerType_04()
        {
            string source = """
                using System.Collections;
                C c;
                c = [];
                c = [1, 2];
                class C : IEnumerable
                {
                    C(object o) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    public void Add(int i) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("C", "0").WithLocation(3, 5),
                // (4,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [1, 2];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[1, 2]").WithArguments("C", "0").WithLocation(4, 5));
        }

        [Fact]
        public void CollectionInitializerType_05()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class A : IEnumerable<int>
                {
                    A() { }
                    public void Add(int i) { }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    static A Create1() => [];
                }
                class B
                {
                    static A Create2() => [1, 2];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,27): error CS0122: 'A.A()' is inaccessible due to its protection level
                //     static A Create2() => [1, 2];
                Diagnostic(ErrorCode.ERR_BadAccess, "[1, 2]").WithArguments("A.A()").WithLocation(13, 27));
        }

        [Fact]
        public void CollectionInitializerType_06()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    private List<T> _list = new List<T>();
                    public void Add(T t) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c;
                        object o;
                        c = [];
                        o = (C<object>)[];
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C<object>)[3, 4];
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_ConstructorOptionalParameters()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable<int>
                {
                    private List<int> _list = new List<int>();
                    internal C(int x = 1, int y = 2) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C c;
                        object o;
                        c = [];
                        o = (C)[];
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C)[3, 4];
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_ConstructorParamsArray()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable<int>
                {
                    private List<int> _list = new List<int>();
                    internal C(params int[] args) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C c;
                        object o;
                        c = [];
                        o = (C)[];
                        c.Report();
                        o.Report();
                        c = [1, 2];
                        o = (C)[3, 4];
                        c.Report();
                        o.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        [Fact]
        public void CollectionInitializerType_07()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                abstract class A : IEnumerable<int>
                {
                    public void Add(int i) { }
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class B : A { }
                class Program
                {
                    static void Main()
                    {
                        A a = [];
                        B b = [];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,15): error CS0144: Cannot create an instance of the abstract type or interface 'A'
                //         A a = [];
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "[]").WithArguments("A").WithLocation(14, 15));
        }

        [Fact]
        public void CollectionInitializerType_08()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                struct S0<T> : IEnumerable
                {
                    public void Add(T t) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S1<T> : IEnumerable<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S2<T> : IEnumerable<T>
                {
                    public S2() { }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void M0()
                    {
                        object o = (S0<int>)[];
                        S0<int> s = [1, 2];
                    }
                    static void M1()
                    {
                        object o = (S1<int>)[];
                        S1<int> s = [1, 2];
                    }
                    static void M2()
                    {
                        S2<int> s = [];
                        object o = (S2<int>)[1, 2];
                    }
                }
                """;
            var verifier = CompileAndVerify(source);
            verifier.VerifyIL("Program.M0", """
                {
                  // Code size       35 (0x23)
                  .maxstack  2
                  .locals init (S0<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S0<int>"
                  IL_0008:  ldloc.0
                  IL_0009:  pop
                  IL_000a:  ldloca.s   V_0
                  IL_000c:  initobj    "S0<int>"
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldc.i4.1
                  IL_0015:  call       "void S0<int>.Add(int)"
                  IL_001a:  ldloca.s   V_0
                  IL_001c:  ldc.i4.2
                  IL_001d:  call       "void S0<int>.Add(int)"
                  IL_0022:  ret
                }
                """);
            verifier.VerifyIL("Program.M1", """
                {
                  // Code size       35 (0x23)
                  .maxstack  2
                  .locals init (S1<int> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "S1<int>"
                  IL_0008:  ldloc.0
                  IL_0009:  pop
                  IL_000a:  ldloca.s   V_0
                  IL_000c:  initobj    "S1<int>"
                  IL_0012:  ldloca.s   V_0
                  IL_0014:  ldc.i4.1
                  IL_0015:  call       "void S1<int>.Add(int)"
                  IL_001a:  ldloca.s   V_0
                  IL_001c:  ldc.i4.2
                  IL_001d:  call       "void S1<int>.Add(int)"
                  IL_0022:  ret
                }
                """);
            verifier.VerifyIL("Program.M2", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  2
                  .locals init (S2<int> V_0)
                  IL_0000:  newobj     "S2<int>..ctor()"
                  IL_0005:  pop
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  call       "S2<int>..ctor()"
                  IL_000d:  ldloca.s   V_0
                  IL_000f:  ldc.i4.1
                  IL_0010:  call       "void S2<int>.Add(int)"
                  IL_0015:  ldloca.s   V_0
                  IL_0017:  ldc.i4.2
                  IL_0018:  call       "void S2<int>.Add(int)"
                  IL_001d:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_09()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        UnknownType u;
                        u = [];
                        u = [null, B];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS0246: The type or namespace name 'UnknownType' could not be found (are you missing a using directive or an assembly reference?)
                //         UnknownType u;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownType").WithArguments("UnknownType").WithLocation(7, 9),
                // (9,20): error CS0103: The name 'B' does not exist in the current context
                //         u = [null, B];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(9, 20));
        }

        [Fact]
        public void CollectionInitializerType_10()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                struct S<T> : IEnumerable<string>
                {
                    public void Add(string i) { }
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        S<UnknownType> s;
                        s = [];
                        s = [null, B];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,11): error CS0246: The type or namespace name 'UnknownType' could not be found (are you missing a using directive or an assembly reference?)
                //         S<UnknownType> s;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownType").WithArguments("UnknownType").WithLocation(13, 11),
                // (15,20): error CS0103: The name 'B' does not exist in the current context
                //         s = [null, B];
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(15, 20));
        }

        [Fact]
        public void CollectionInitializerType_11()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<List<int>> l;
                        l = [[], [2, 3]];
                        l = [[], {2, 3}];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,18): error CS1003: Syntax error, ']' expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(8, 18),
                // (8,18): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(8, 18),
                // (8,20): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 20),
                // (8,20): error CS1513: } expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 20),
                // (8,23): error CS1002: ; expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(8, 23),
                // (8,24): error CS1513: } expected
                //         l = [[], {2, 3}];
                Diagnostic(ErrorCode.ERR_RbraceExpected, "]").WithLocation(8, 24));
        }

        [Fact]
        public void CollectionInitializerType_12()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    List<string> _list = new List<string>();
                    public void Add(int i) { _list.Add($"i={i}"); }
                    public void Add(object o) { _list.Add($"o={o}"); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C x = [];
                        C y = [1, (object)2];
                        x.Report();
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [i=1, o=2], ");
        }

        [Fact]
        public void CollectionInitializerType_13()
        {
            string source = """
                using System.Collections;
                interface IA { }
                interface IB { }
                class AB : IA, IB { }
                class C : IEnumerable
                {
                    public void Add(IA a) { }
                    public void Add(IB b) { }
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        C c = [(IA)null, (IB)null, new AB()];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (15,36): error CS0121: The call is ambiguous between the following methods or properties: 'C.Add(IA)' and 'C.Add(IB)'
                //         C c = [(IA)null, (IB)null, new AB()];
                Diagnostic(ErrorCode.ERR_AmbigCall, "new AB()").WithArguments("C.Add(IA)", "C.Add(IB)").WithLocation(15, 36));
        }

        [Fact]
        public void CollectionInitializerType_14()
        {
            string source = """
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T x, T y) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void Main()
                    {
                        S<int> s;
                        s = [];
                        s = [1, 2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,14): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S<int>.Add(int, int)'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "1").WithArguments("y", "S<int>.Add(int, int)").WithLocation(13, 14),
                // (13,17): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S<int>.Add(int, int)'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "2").WithArguments("y", "S<int>.Add(int, int)").WithLocation(13, 17));
        }

        [Fact]
        public void CollectionInitializerType_15()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(T t, int index = -1) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [1, 2];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_16()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(T t, params T[] args) { _list.Add(t); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [1, 2];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], ");
        }

        [Fact]
        public void CollectionInitializerType_17()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    List<T> _list = new List<T>();
                    public void Add(params T[] args) { _list.AddRange(args); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> c = [[], [1, 2], 3];
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2, 3], ");
            verifier.VerifyIL("Program.Main", """
                {
                  // Code size       62 (0x3e)
                  .maxstack  5
                  .locals init (C<int> V_0)
                  IL_0000:  newobj     "C<int>..ctor()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloc.0
                  IL_0007:  ldc.i4.0
                  IL_0008:  newarr     "int"
                  IL_000d:  callvirt   "void C<int>.Add(params int[])"
                  IL_0012:  ldloc.0
                  IL_0013:  ldc.i4.2
                  IL_0014:  newarr     "int"
                  IL_0019:  dup
                  IL_001a:  ldc.i4.0
                  IL_001b:  ldc.i4.1
                  IL_001c:  stelem.i4
                  IL_001d:  dup
                  IL_001e:  ldc.i4.1
                  IL_001f:  ldc.i4.2
                  IL_0020:  stelem.i4
                  IL_0021:  callvirt   "void C<int>.Add(params int[])"
                  IL_0026:  ldloc.0
                  IL_0027:  ldc.i4.1
                  IL_0028:  newarr     "int"
                  IL_002d:  dup
                  IL_002e:  ldc.i4.0
                  IL_002f:  ldc.i4.3
                  IL_0030:  stelem.i4
                  IL_0031:  callvirt   "void C<int>.Add(params int[])"
                  IL_0036:  ldloc.0
                  IL_0037:  ldc.i4.0
                  IL_0038:  call       "void CollectionExtensions.Report(object, bool)"
                  IL_003d:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_18()
        {
            string source = """
                using System.Collections;
                class S<T, U> : IEnumerable
                {
                    internal void Add(T t) { }
                    private void Add(U u) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    static S<T, U> Create(T t, U u) => [t, u];
                }
                class Program
                {
                    static S<T, U> Create<T, U>(T x, U y) => [x, y];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (11,50): error CS1950: The best overloaded Add method 'S<T, U>.Add(T)' for the collection initializer has some invalid arguments
                //     static S<T, U> Create<T, U>(T x, U y) => [x, y];
                Diagnostic(ErrorCode.ERR_BadArgTypesForCollectionAdd, "y").WithArguments("S<T, U>.Add(T)").WithLocation(11, 50),
                // (11,50): error CS1503: Argument 1: cannot convert from 'U' to 'T'
                //     static S<T, U> Create<T, U>(T x, U y) => [x, y];
                Diagnostic(ErrorCode.ERR_BadArgType, "y").WithArguments("1", "U", "T").WithLocation(11, 50));
        }

        [Fact]
        public void CollectionInitializerType_19()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        string s;
                        s = [];
                        s = ['a'];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         s = [];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("string", "0").WithLocation(6, 13),
                // (7,13): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         s = ['a'];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "['a']").WithArguments("string", "0").WithLocation(7, 13),
                // (7,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         s = ['a'];
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(7, 14));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public void TypeParameter_01(string type)
        {
            string source = $$"""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable
                {
                    void Add(T t);
                }
                {{type}} C<T> : I<T>
                {
                    private List<T> _list;
                    public void Add(T t)
                    {
                        GetList().Add(t);
                    }
                    IEnumerator IEnumerable.GetEnumerator()
                    {
                        return GetList().GetEnumerator();
                    }
                    private List<T> GetList() => _list ??= new List<T>();
                }
                class Program
                {
                    static void Main()
                    {
                        CreateEmpty<C<object>, object>().Report();
                        Create<C<long?>, long?>(null, 2).Report();
                    }
                    static T CreateEmpty<T, U>() where T : I<U>, new()
                    {
                        return [];
                    }
                    static T Create<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return [a, b];
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[], [null, 2], ");
            verifier.VerifyIL("Program.CreateEmpty<T, U>", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "T System.Activator.CreateInstance<T>()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.Create<T, U>", """
                {
                  // Code size       36 (0x24)
                  .maxstack  2
                  .locals init (T V_0)
                  IL_0000:  call       "T System.Activator.CreateInstance<T>()"
                  IL_0005:  stloc.0
                  IL_0006:  ldloca.s   V_0
                  IL_0008:  ldarg.0
                  IL_0009:  constrained. "T"
                  IL_000f:  callvirt   "void I<U>.Add(U)"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldarg.1
                  IL_0017:  constrained. "T"
                  IL_001d:  callvirt   "void I<U>.Add(U)"
                  IL_0022:  ldloc.0
                  IL_0023:  ret
                }
                """);
        }

        [Fact]
        public void TypeParameter_02()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static T Create1<T, U>() where T : struct, I<U> => [];
                    static T? Create2<T, U>() where T : struct, I<U> => [];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (16,57): error CS9500: Cannot initialize type 'T?' with a collection literal because the type is not constructible.
                //     static T? Create2<T, U>() where T : struct, I<U> => [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("T?").WithLocation(16, 57));
        }

        [Fact]
        public void TypeParameter_03()
        {
            string source = """
                using System.Collections;
                class Program
                {
                    static T Create1<T, U>() where T : IEnumerable => []; // 1
                    static T Create2<T, U>() where T : class, IEnumerable => []; // 2
                    static T Create3<T, U>() where T : struct, IEnumerable => [];
                    static T Create4<T, U>() where T : IEnumerable, new() => [];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,55): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create1<T, U>() where T : IEnumerable => []; // 1
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[]").WithArguments("T").WithLocation(4, 55),
                // (5,62): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create2<T, U>() where T : class, IEnumerable => []; // 2
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[]").WithArguments("T").WithLocation(5, 62));
        }

        [Fact]
        public void TypeParameter_04()
        {
            string source = """
                using System.Collections;
                interface IAdd : IEnumerable
                {
                    void Add(int i);
                }
                class Program
                {
                    static T Create1<T>() where T : IAdd => [1]; // 1
                    static T Create2<T>() where T : class, IAdd => [2]; // 2
                    static T Create3<T>() where T : struct, IAdd => [3];
                    static T Create4<T>() where T : IAdd, new() => [4];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,45): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create1<T>() where T : IAdd => [1]; // 1
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[1]").WithArguments("T").WithLocation(8, 45),
                // (9,52): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //     static T Create2<T>() where T : class, IAdd => [2]; // 2
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "[2]").WithArguments("T").WithLocation(9, 52));
        }

        [Fact]
        public void CollectionInitializerType_MissingIEnumerable()
        {
            string source = """
                struct S
                {
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        object o = (S)[1, 2];
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_Collections_IEnumerable);
            comp.VerifyEmitDiagnostics(
                // (8,15): error CS0518: Predefined type 'System.Collections.IEnumerable' is not defined or imported
                //         S s = [];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Collections.IEnumerable").WithLocation(8, 15),
                // (8,15): error CS9500: Cannot initialize type 'S' with a collection literal because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(8, 15),
                // (9,23): error CS0518: Predefined type 'System.Collections.IEnumerable' is not defined or imported
                //         object o = (S)[1, 2];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[1, 2]").WithArguments("System.Collections.IEnumerable").WithLocation(9, 23),
                // (9,23): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                //         object o = (S)[1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(9, 23));
        }

        [Fact]
        public void CollectionInitializerType_UseSiteErrors()
        {
            string assemblyA = GetUniqueName();
            string sourceA = """
                public class A1 { }
                public class A2 { }
                """;
            var comp = CreateCompilation(sourceA, assemblyName: assemblyA);
            var refA = comp.EmitToImageReference();

            string sourceB = """
                using System.Collections;
                using System.Collections.Generic;
                public class B1 : IEnumerable
                {
                    List<int> _list = new List<int>();
                    public B1(A1 a = null) { }
                    public void Add(int i) { _list.Add(i); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                public class B2 : IEnumerable
                {
                    List<int> _list = new List<int>();
                    public void Add(int x, A2 y = null) { _list.Add(x); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                """;
            comp = CreateCompilation(sourceB, references: new[] { refA });
            var refB = comp.EmitToImageReference();

            string sourceC = """
                class C
                {
                    static void Main()
                    {
                        B1 x;
                        x = [];
                        x.Report();
                        x = [1, 2];
                        x.Report();
                        B2 y;
                        y = [];
                        y.Report();
                        y = [3, 4];
                        y.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { sourceC, s_collectionExtensions }, references: new[] { refA, refB }, expectedOutput: "[], [1, 2], [], [3, 4], ");

            comp = CreateCompilation(new[] { sourceC, s_collectionExtensions }, references: new[] { refB });
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS0012: The type 'A1' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x = [];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "[]").WithArguments("A1", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 13),
                // (8,13): error CS0012: The type 'A1' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         x = [1, 2];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "[1, 2]").WithArguments("A1", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(8, 13),
                // (13,14): error CS0012: The type 'A2' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y = [3, 4];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "3").WithArguments("A2", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(13, 14),
                // (13,17): error CS0012: The type 'A2' is defined in an assembly that is not referenced. You must add a reference to assembly 'a897d975-a839-4fff-828b-deccf9495adc, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         y = [3, 4];
                Diagnostic(ErrorCode.ERR_NoTypeDef, "4").WithArguments("A2", $"{assemblyA}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(13, 17));
        }

        [Fact]
        public void DictionaryElement_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Dictionary<int, int> d;
                        d = [];
                        d = [new KeyValuePair<int, int>(1, 2)];
                        d = [3:4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,14): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'Dictionary<int, int>.Add(int, int)'
                //         d = [new KeyValuePair<int, int>(1, 2)];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new KeyValuePair<int, int>(1, 2)").WithArguments("value", "System.Collections.Generic.Dictionary<int, int>.Add(int, int)").WithLocation(8, 14),
                // (9,14): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         d = [3:4];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "3:4").WithLocation(9, 14));
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [InlineData("int[]")]
        [InlineData("System.Collections.Generic.List<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<int>")]
        public void SpreadElement_01(string collectionType)
        {
            string source = $$"""
                class Program
                {
                    static void Main()
                    {
                        {{collectionType}} c;
                        c = [];
                        c = [..c, ..[1, 2]];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         c = [..c, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..c").WithLocation(7, 14),
                // (7,19): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         c = [..c, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..[1, 2]").WithLocation(7, 19));
        }

        [Fact]
        public void SpreadElement_02()
        {
            string source = """
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    public T this[int index] => default;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        S<int> s;
                        s = [];
                        s = [..s, ..[1, 2]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,14): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         s = [..s, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..s").WithLocation(14, 14),
                // (14,19): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         s = [..s, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..[1, 2]").WithLocation(14, 19));
        }

        [Fact]
        public void Nullable_01()
        {
            string source = """
                #nullable enable
                class Program
                {
                    static void Main()
                    {
                        object?[] x = [1];
                        x[0].ToString(); // 1
                        object[] y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                        object[]? z = [];
                        z.ToString();
                        z = [3];
                        z.ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: // 2 should be reported as a warning (compare with array initializer: new object[] { null }).
            comp.VerifyEmitDiagnostics(
                // (7,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(7, 9));
        }

        [Fact]
        public void Nullable_02()
        {
            string source = """
                #nullable enable
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object?> x = [1];
                        x[0].ToString(); // 1
                        List<object> y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                        List<object>? z = [];
                        z.ToString();
                        z = [3];
                        z.ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(8, 9),
                // (9,27): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         List<object> y = [null]; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 27),
                // (11,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         y = [2, null]; // 3
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 17));
        }

        [Fact]
        public void Nullable_03()
        {
            string source = """
                #nullable enable
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    public T this[int index] => default!;
                    IEnumerator IEnumerable.GetEnumerator() => default!;
                }
                class Program
                {
                    static void Main()
                    {
                        S<object?> x = [1];
                        x[0].ToString(); // 1
                        S<object> y = [null]; // 2
                        y[0].ToString();
                        y = [2, null]; // 3
                        y[1].ToString();
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         x[0].ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x[0]").WithLocation(14, 9),
                // (15,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         S<object> y = [null]; // 2
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 24),
                // (17,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         y = [2, null]; // 3
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(17, 17));
        }

        [Fact]
        public void Nullable_04()
        {
            string source = """
                #nullable enable
                using System.Collections;
                struct S<T> : IEnumerable
                {
                    public void Add(T t) { }
                    public T this[int index] => default!;
                    IEnumerator IEnumerable.GetEnumerator() => default!;
                }
                class Program
                {
                    static void Main()
                    {
                        S<object>? x = [];
                        x = [];
                        S<object>? y = [1];
                        y = [2];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS9500: Cannot initialize type 'S<object>?' with a collection literal because the type is not constructible.
                //         S<object>? x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S<object>?").WithLocation(13, 24),
                // (14,13): error CS9500: Cannot initialize type 'S<object>?' with a collection literal because the type is not constructible.
                //         x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S<object>?").WithLocation(14, 13),
                // (15,24): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S<object>?'
                //         S<object>? y = [1];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1]").WithArguments("System.Collections.Generic.List<int>", "S<object>?").WithLocation(15, 24),
                // (16,13): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S<object>?'
                //         y = [2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[2]").WithArguments("System.Collections.Generic.List<int>", "S<object>?").WithLocation(16, 13));
        }

        [Fact]
        public void OrderOfEvaluation()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class C<T> : IEnumerable
                {
                    private List<T> _list = new List<T>();
                    public void Add(T t)
                    {
                        Console.WriteLine("Add {0}", t);
                        _list.Add(t);
                    }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class Program
                {
                    static void Main()
                    {
                        C<int> x = [Get(1), Get(2)];
                        C<C<int>> y = [[Get(3)], [Get(4), Get(5)]];
                    }
                    static int Get(int value)
                    {
                        Console.WriteLine("Get {0}", value);
                        return value;
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: """
                Get 1
                Add 1
                Get 2
                Add 2
                Get 3
                Add 3
                Add C`1[System.Int32]
                Get 4
                Add 4
                Get 5
                Add 5
                Add C`1[System.Int32]
                """);
        }

        // Ensure collection literal conversions are not standard implicit conversions
        // and, as a result, are ignored when determining user-defined conversions.
        [Fact]
        public void UserDefinedConversions_01()
        {
            string source = """
                struct S
                {
                    public static implicit operator S(int[] a) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        s = [1, 2];
                        s = (S)[3, 4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,15): error CS9500: Cannot initialize type 'S' with a collection literal because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(9, 15),
                // (10,13): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(10, 13),
                // (11,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                //         s = (S)[3, 4];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[3, 4]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(11, 16));
        }

        [Fact]
        public void UserDefinedConversions_02()
        {
            string source = """
                struct S
                {
                    public static explicit operator S(int[] a) => default;
                }
                class Program
                {
                    static void Main()
                    {
                        S s = [];
                        s = [1, 2];
                        s = (S)[3, 4];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,15): error CS9500: Cannot initialize type 'S' with a collection literal because the type is not constructible.
                //         S s = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S").WithLocation(9, 15),
                // (10,13): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                //         s = [1, 2];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[1, 2]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(10, 13),
                // (11,16): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<int>' to 'S'
                //         s = (S)[3, 4];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "[3, 4]").WithArguments("System.Collections.Generic.List<int>", "S").WithLocation(11, 16));
        }

        [Fact]
        public void PrimaryConstructorParameters_01()
        {
            string source = """
                struct S(int x, int y, int z)
                {
                    int[] F = [x, y];
                    int[] M() => [y];
                    static void Main()
                    {
                        var s = new S(1, 2, 3);
                        s.F.Report();
                        s.M().Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(1,28): warning CS9113: Parameter 'z' is unread.
                // struct S(int x, int y, int z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "z").WithArguments("z").WithLocation(1, 28));

            var verifier = CompileAndVerify(comp, expectedOutput: "[1, 2], [2], ");
            verifier.VerifyIL("S..ctor(int, int, int)",
                """
                {
                  // Code size       33 (0x21)
                  .maxstack  5
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.2
                  IL_0002:  stfld      "int S.<y>PC__BackingField"
                  IL_0007:  ldarg.0
                  IL_0008:  ldc.i4.2
                  IL_0009:  newarr     "int"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.0
                  IL_0010:  ldarg.1
                  IL_0011:  stelem.i4
                  IL_0012:  dup
                  IL_0013:  ldc.i4.1
                  IL_0014:  ldarg.0
                  IL_0015:  ldfld      "int S.<y>PC__BackingField"
                  IL_001a:  stelem.i4
                  IL_001b:  stfld      "int[] S.F"
                  IL_0020:  ret
                }
                """);
        }

        [Fact]
        public void PrimaryConstructorParameters_02()
        {
            string source = """
                using System;
                class C(int x, int y, int z)
                {
                    Func<int[]> F = () => [x, y];
                    Func<int[]> M() => () => [y];
                    static void Main()
                    {
                        var c = new C(1, 2, 3);
                        c.F().Report();
                        c.M()().Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(2,27): warning CS9113: Parameter 'z' is unread.
                // class C(int x, int y, int z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "z").WithArguments("z").WithLocation(2, 27));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "[1, 2], [2], ");
            verifier.VerifyIL("C..ctor(int, int, int)",
                """
                {
                  // Code size       52 (0x34)
                  .maxstack  3
                  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.2
                  IL_0002:  stfld      "int C.<y>PC__BackingField"
                  IL_0007:  newobj     "C.<>c__DisplayClass0_0..ctor()"
                  IL_000c:  stloc.0
                  IL_000d:  ldloc.0
                  IL_000e:  ldarg.1
                  IL_000f:  stfld      "int C.<>c__DisplayClass0_0.x"
                  IL_0014:  ldloc.0
                  IL_0015:  ldarg.0
                  IL_0016:  stfld      "C C.<>c__DisplayClass0_0.<>4__this"
                  IL_001b:  ldarg.0
                  IL_001c:  ldloc.0
                  IL_001d:  ldftn      "int[] C.<>c__DisplayClass0_0.<.ctor>b__0()"
                  IL_0023:  newobj     "System.Func<int[]>..ctor(object, System.IntPtr)"
                  IL_0028:  stfld      "System.Func<int[]> C.F"
                  IL_002d:  ldarg.0
                  IL_002e:  call       "object..ctor()"
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void PrimaryConstructorParameters_03()
        {
            string source = """
                using System.Collections.Generic;
                class A(int[] x, List<int> y)
                {
                    public int[] X = x;
                    public List<int> Y = y;
                }
                class B(int x, int y, int z) : A([y, z], [z])
                {
                }
                class Program
                {
                    static void Main()
                    {
                        var b = new B(1, 2, 3);
                        b.X.Report();
                        b.Y.Report();
                    }
                }
                """;

            var comp = CreateCompilation(new[] { source, s_collectionExtensions }, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,13): warning CS9113: Parameter 'x' is unread.
                // class B(int x, int y, int z) : A([y, z], [z])
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(7, 13));

            var verifier = CompileAndVerify(comp, expectedOutput: "[2, 3], [3], ");
            verifier.VerifyIL("B..ctor(int, int, int)",
                """
                {
                  // Code size       33 (0x21)
                  .maxstack  5
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.2
                  IL_0002:  newarr     "int"
                  IL_0007:  dup
                  IL_0008:  ldc.i4.0
                  IL_0009:  ldarg.2
                  IL_000a:  stelem.i4
                  IL_000b:  dup
                  IL_000c:  ldc.i4.1
                  IL_000d:  ldarg.3
                  IL_000e:  stelem.i4
                  IL_000f:  newobj     "System.Collections.Generic.List<int>..ctor()"
                  IL_0014:  dup
                  IL_0015:  ldarg.3
                  IL_0016:  callvirt   "void System.Collections.Generic.List<int>.Add(int)"
                  IL_001b:  call       "A..ctor(int[], System.Collections.Generic.List<int>)"
                  IL_0020:  ret
                }
                """);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void SemanticModel()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                struct S1 : IEnumerable
                {
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                struct S2
                {
                }
                class Program
                {
                    static void Main()
                    {
                        int[] v1 = [];
                        List<object> v2 = [];
                        Span<int> v3 = [];
                        ReadOnlySpan<object> v4 = [];
                        S1 v5 = [];
                        S2 v6 = [];
                        var v7 = (int[])[];
                        var v8 = (List<object>)[];
                        var v9 = (Span<int>)[];
                        var v10 = (ReadOnlySpan<object>)[];
                        var v11 = (S1)[];
                        var v12 = (S2)[];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (20,17): error CS9500: Cannot initialize type 'S2' with a collection literal because the type is not constructible.
                //         S2 v6 = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S2").WithLocation(20, 17),
                // (26,23): error CS9500: Cannot initialize type 'S2' with a collection literal because the type is not constructible.
                //         var v12 = (S2)[];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("S2").WithLocation(26, 23));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionCreationExpressionSyntax>().ToArray();
            Assert.Equal(12, collections.Length);
            verifyTypes(collections[0], "System.Int32[]", ConversionKind.CollectionLiteral);
            verifyTypes(collections[1], "System.Collections.Generic.List<System.Object>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[2], "System.Span<System.Int32>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[3], "System.ReadOnlySpan<System.Object>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[4], "S1", ConversionKind.CollectionLiteral);
            verifyTypes(collections[5], "S2", ConversionKind.CollectionLiteral);
            verifyTypes(collections[6], "System.Int32[]", ConversionKind.CollectionLiteral);
            verifyTypes(collections[7], "System.Collections.Generic.List<System.Object>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[8], "System.Span<System.Int32>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[9], "System.ReadOnlySpan<System.Object>", ConversionKind.CollectionLiteral);
            verifyTypes(collections[10], "S1", ConversionKind.CollectionLiteral);
            verifyTypes(collections[11], "S2", ConversionKind.CollectionLiteral);

            // PROTOTYPE: Test cases that are not target typed.

            void verifyTypes(ExpressionSyntax expr, string expectedConvertedType, ConversionKind expectedConversionKind)
            {
                var typeInfo = model.GetTypeInfo(expr);
                var conversion = model.GetConversion(expr);
                Assert.Null(typeInfo.Type);
                Assert.Equal(expectedConvertedType, typeInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(expectedConversionKind, conversion.Kind);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void RestrictedTypes()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var x = [default(TypedReference)];
                        var y = [default(ArgIterator)];
                        var z = [default(RuntimeArgumentHandle)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS0611: Array elements cannot be of type 'TypedReference'
                //         var x = [default(TypedReference)];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "[default(TypedReference)]").WithArguments("System.TypedReference").WithLocation(6, 17),
                // (6,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var x = [default(TypedReference)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[default(TypedReference)]").WithLocation(6, 17),
                // (7,17): error CS0611: Array elements cannot be of type 'ArgIterator'
                //         var y = [default(ArgIterator)];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "[default(ArgIterator)]").WithArguments("System.ArgIterator").WithLocation(7, 17),
                // (7,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var y = [default(ArgIterator)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[default(ArgIterator)]").WithLocation(7, 17),
                // (8,17): error CS0611: Array elements cannot be of type 'RuntimeArgumentHandle'
                //         var z = [default(RuntimeArgumentHandle)];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "[default(RuntimeArgumentHandle)]").WithArguments("System.RuntimeArgumentHandle").WithLocation(8, 17),
                // (8,17): error CS9503: No best type found for implicitly-typed collection literal.
                //         var z = [default(RuntimeArgumentHandle)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedCollectionLiteralNoBestType, "[default(RuntimeArgumentHandle)]").WithLocation(8, 17));
        }

        [Fact]
        public void RefStruct_01()
        {
            string source = """
                ref struct R
                {
                    public R(ref int i) { }
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 0;
                        var x = [default(R)];
                        var y = [new R(ref i)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Report error for List<R>.
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void RefStruct_02()
        {
            string source = """
                using System.Collections.Generic;
                ref struct R
                {
                    public int _i;
                    public R(ref int i) { _i = i; }
                    public static implicit operator int(R r) => r._i;
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 1;
                        int[] a = [default(R), new R(ref i)];
                        a.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[0, 1], ");
        }

        [Fact]
        public void RefStruct_03()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class C : IEnumerable
                {
                    private List<int> _list = new List<int>();
                    public void Add(R r) { _list.Add(r._i); }
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                ref struct R
                {
                    public int _i;
                    public R(ref int i) { _i = i; }
                }
                class Program
                {
                    static void Main()
                    {
                        int i = 1;
                        C c = [default(R), new R(ref i)];
                        c.Report();
                    }
                }
                """;
            CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[0, 1], ");
        }

        [Fact]
        public void ExpressionTrees()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                interface I<T> : IEnumerable
                {
                    void Add(T t);
                }
                class Program
                {
                    static Expression<Func<int[]>> Create1()
                    {
                        return () => [];
                    }
                    static Expression<Func<List<object>>> Create2()
                    {
                        return () => [1, 2];
                    }
                    static Expression<Func<T>> Create3<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return () => [a, b];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,22): error CS9501: An expression tree may not contain a collection literal.
                //         return () => [];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionLiteral, "[]").WithLocation(13, 22),
                // (17,22): error CS9501: An expression tree may not contain a collection literal.
                //         return () => [1, 2];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionLiteral, "[1, 2]").WithLocation(17, 22),
                // (21,22): error CS9501: An expression tree may not contain a collection literal.
                //         return () => [a, b];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionLiteral, "[a, b]").WithLocation(21, 22));
        }

        [Fact]
        public void IOperation_Array()
        {
            string source = """
                class Program
                {
                    static T[] Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionCreationExpressionSyntax>(comp,
@"IArrayCreationOperation (OperationKind.ArrayCreation, Type: T[]) (Syntax: '[a, b]')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '[a, b]')
  Initializer:
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '[a, b]')
      Element Values(2):
          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
");

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Return) Block[B2]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T[], IsImplicit) (Syntax: '[a, b]')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (CollectionLiteral)
          Operand:
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: T[]) (Syntax: '[a, b]')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '[a, b]')
              Initializer:
                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '[a, b]')
                  Element Values(2):
                      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void IOperation_Span()
        {
            string source = """
                using System;
                class Program
                {
                    static Span<T> Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionCreationExpressionSyntax>(comp,
@"IObjectCreationOperation (Constructor: System.Span<T>..ctor(T[]? array)) (OperationKind.ObjectCreation, Type: System.Span<T>) (Syntax: '[a, b]')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[a, b]')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: T[]?, IsImplicit) (Syntax: '[a, b]')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '[a, b]')
          Initializer:
            IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '[a, b]')
              Element Values(2):
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Initializer:
    null
");

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Return) Block[B2]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<T>, IsImplicit) (Syntax: '[a, b]')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (CollectionLiteral)
          Operand:
            IObjectCreationOperation (Constructor: System.Span<T>..ctor(T[]? array)) (OperationKind.ObjectCreation, Type: System.Span<T>) (Syntax: '[a, b]')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[a, b]')
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: T[]?, IsImplicit) (Syntax: '[a, b]')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '[a, b]')
                      Initializer:
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '[a, b]')
                          Element Values(2):
                              IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'a')
                              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T, IsImplicit) (Syntax: 'b')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer:
                null
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact]
        public void IOperation_CollectionInitializer()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static S<T> Create<T>(T a, T b)
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionCreationExpressionSyntax>(comp,
@"IObjectCreationOperation (Constructor: S<T>..ctor()) (OperationKind.ObjectCreation, Type: S<T>) (Syntax: '[a, b]')
  Arguments(0)
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
      Initializers(2):
          IInvocationOperation ( void S<T>.Add(T t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a')
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T) (Syntax: 'a')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( void S<T>.Add(T t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T) (Syntax: 'b')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[a, b]')
              Value:
                IObjectCreationOperation (Constructor: S<T>..ctor()) (OperationKind.ObjectCreation, Type: S<T>) (Syntax: '[a, b]')
                  Arguments(0)
                  Initializer:
                    null
            IInvocationOperation ( void S<T>.Add(T t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a')
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: T) (Syntax: 'a')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void S<T>.Add(T t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: T) (Syntax: 'b')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (CollectionLiteral)
              Operand:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S<T>, IsImplicit) (Syntax: '[a, b]')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact]
        public void IOperation_TypeParameter()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                interface I<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct S<T> : I<T>
                {
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static T Create<T, U>(U a, U b) where T : I<U>, new()
                    {
                        return /*<bind>*/[a, b]/*</bind>*/;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionCreationExpressionSyntax>(comp,
@"ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: '[a, b]')
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T, IsImplicit) (Syntax: '[a, b]')
      Initializers(2):
          IInvocationOperation (virtual void I<U>.Add(U t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: '[a, b]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a')
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: U) (Syntax: 'a')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation (virtual void I<U>.Add(U t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: '[a, b]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: U) (Syntax: 'b')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Create");
            VerifyFlowGraph(comp, method,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[a, b]')
              Value:
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: '[a, b]')
                  Initializer:
                    null
            IInvocationOperation (virtual void I<U>.Add(U t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: '[a, b]')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'a')
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: U) (Syntax: 'a')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation (virtual void I<U>.Add(U t)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: '[a, b]')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: U) (Syntax: 'b')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Return) Block[B2]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsImplicit) (Syntax: '[a, b]')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (CollectionLiteral)
              Operand:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: '[a, b]')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact]
        public void IOperation_Nested()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<List<int>> x = /*<bind>*/[[Get(1)]]/*</bind>*/;
                    }
                    static int Get(int value) => value;
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            VerifyOperationTreeForTest<CollectionCreationExpressionSyntax>(comp,
@"IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '[[Get(1)]]')
  Arguments(0)
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: '[[Get(1)]]')
      Initializers(1):
          IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '[Get(1)]')
            Instance Receiver:
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: '[[Get(1)]]')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[Get(1)]')
                  IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[Get(1)]')
                    Arguments(0)
                    Initializer:
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                        Initializers(1):
                            IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Get(1)')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Get(1)')
                                    IInvocationOperation (System.Int32 Program.Get(System.Int32 value)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get(1)')
                                      Instance Receiver:
                                        null
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '1')
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var tree = comp.SyntaxTrees[0];
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "Main");
            VerifyFlowGraph(comp, method,
@"Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[[Get(1)]]')
              Value:
                IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>) (Syntax: '[[Get(1)]]')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[Get(1)]')
                  Value:
                    IObjectCreationOperation (Constructor: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List<System.Int32>) (Syntax: '[Get(1)]')
                      Arguments(0)
                      Initializer:
                        null
                IInvocationOperation ( void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Get(1)')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Get(1)')
                        IInvocationOperation (System.Int32 Program.Get(System.Int32 value)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get(1)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '1')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation ( void System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>.Add(System.Collections.Generic.List<System.Int32> item)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '[Get(1)]')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: '[[Get(1)]]')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[Get(1)]')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: '[Get(1)]')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x = /*<bind>*/[[Get(1)]]')
              Left:
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: 'x = /*<bind>*/[[Get(1)]]')
              Right:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, IsImplicit) (Syntax: '[[Get(1)]]')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
");
        }

        [Fact]
        public void Async()
        {
            string source = """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                class Program
                {
                    static async Task Main()
                    {
                        (await CreateArray()).Report();
                        (await CreateList()).Report();
                    }
                    static async Task<int[]> CreateArray()
                    {
                        return [await F(1), await F(2)];
                    }
                    static async Task<List<int>> CreateList()
                    {
                        return [await F(3), await F(4)];
                    }
                    static async Task<int> F(int i)
                    {
                        Task.Yield();
                        return i;
                    }
                }
                """;
            var verifier = CompileAndVerify(new[] { source, s_collectionExtensions }, expectedOutput: "[1, 2], [3, 4], ");
            verifier.VerifyIL("Program.CreateArray", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  2
                  .locals init (Program.<CreateArray>d__1 V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  call       "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]>.Create()"
                  IL_0007:  stfld      "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]> Program.<CreateArray>d__1.<>t__builder"
                  IL_000c:  ldloca.s   V_0
                  IL_000e:  ldc.i4.m1
                  IL_000f:  stfld      "int Program.<CreateArray>d__1.<>1__state"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]> Program.<CreateArray>d__1.<>t__builder"
                  IL_001b:  ldloca.s   V_0
                  IL_001d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]>.Start<Program.<CreateArray>d__1>(ref Program.<CreateArray>d__1)"
                  IL_0022:  ldloca.s   V_0
                  IL_0024:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]> Program.<CreateArray>d__1.<>t__builder"
                  IL_0029:  call       "System.Threading.Tasks.Task<int[]> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int[]>.Task.get"
                  IL_002e:  ret
                }
                """);
            verifier.VerifyIL("Program.CreateList", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  2
                  .locals init (Program.<CreateList>d__2 V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  call       "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>>.Create()"
                  IL_0007:  stfld      "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>> Program.<CreateList>d__2.<>t__builder"
                  IL_000c:  ldloca.s   V_0
                  IL_000e:  ldc.i4.m1
                  IL_000f:  stfld      "int Program.<CreateList>d__2.<>1__state"
                  IL_0014:  ldloca.s   V_0
                  IL_0016:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>> Program.<CreateList>d__2.<>t__builder"
                  IL_001b:  ldloca.s   V_0
                  IL_001d:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>>.Start<Program.<CreateList>d__2>(ref Program.<CreateList>d__2)"
                  IL_0022:  ldloca.s   V_0
                  IL_0024:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>> Program.<CreateList>d__2.<>t__builder"
                  IL_0029:  call       "System.Threading.Tasks.Task<System.Collections.Generic.List<int>> System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Collections.Generic.List<int>>.Task.get"
                  IL_002e:  ret
                }
                """);
        }
    }
}
