﻿// Licensed to the .NET Foundation under one or more agreements.
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
    public class CollectionExpressionArgumentTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        private const string s_collectionExtensions = CollectionExpressionTests.s_collectionExtensions;

        public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersion_01(LanguageVersion languageVersion)
        {
            string source = """
                int[] a = [with()];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (1,12): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // int[] a = [with()];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(1, 12));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersion_02(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                List<int> l = [1, with(), 3, with(capacity: 4)];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (2,19): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 19),
                    // (2,19): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 19),
                    // (2,30): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 30),
                    // (2,30): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 30));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (2,19): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 19),
                    // (2,30): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [1, with(), 3, with(capacity: 4)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 30));
            }
        }

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersion_03(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                List<int> l = [with(x: 1), with(y: 2)];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (2,16): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 16),
                    // (2,21): error CS1739: The best overload for 'List' does not have a parameter named 'x'
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("List", "x").WithLocation(2, 21),
                    // (2,28): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 28),
                    // (2,28): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 28),
                    // (2,33): error CS1739: The best overload for 'List' does not have a parameter named 'y'
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "y").WithArguments("List", "y").WithLocation(2, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (2,21): error CS1739: The best overload for 'List' does not have a parameter named 'x'
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("List", "x").WithLocation(2, 21),
                    // (2,28): error CS9275: Collection argument element must be the first element.
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 28),
                    // (2,33): error CS1739: The best overload for 'List' does not have a parameter named 'y'
                    // List<int> l = [with(x: 1), with(y: 2)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "y").WithArguments("List", "y").WithLocation(2, 33));
            }
        }

        [Fact]
        public void EmptyArguments_Array()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>().Report();
                        EmptyArgs<int>().Report();
                    }
                    static T[] NoArgs<T>() => [];
                    static T[] EmptyArgs<T>() => [with()];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                expectedOutput: "[], [], ");
            verifier.VerifyDiagnostics();
            string expectedIL = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "T[] System.Array.Empty<T>()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T>", expectedIL);
        }

        [Fact]
        public void Arguments_Array()
        {
            string source = """
                class Program
                {
                    static void F<T>(T t)
                    {
                        T[] a;
                        a = [with(default), t];
                        a = [t, with(default)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS9276: Collection arguments are not supported for type 'T[]'.
                //         a = [with(default), t];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("T[]").WithLocation(6, 14),
                // (7,17): error CS9275: Collection argument element must be the first element.
                //         a = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 17),
                // (7,17): error CS9276: Collection arguments are not supported for type 'T[]'.
                //         a = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("T[]").WithLocation(7, 17));

            // Collection arguments do not affect convertibility.
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
            Assert.Equal(2, collections.Length);
            VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: "T[]", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: "T[]", ConversionKind.CollectionExpression);
        }

        private static void VerifyTypes(SemanticModel model, ExpressionSyntax expr, string expectedType, string expectedConvertedType, ConversionKind expectedConversionKind)
        {
            var typeInfo = model.GetTypeInfo(expr);
            var conversion = model.GetConversion(expr);
            Assert.Equal(expectedType, typeInfo.Type?.ToTestDisplayString());
            Assert.Equal(expectedConvertedType, typeInfo.ConvertedType?.ToTestDisplayString());
            Assert.Equal(expectedConversionKind, conversion.Kind);
        }

        [Theory]
        [InlineData("ReadOnlySpan")]
        [InlineData("Span")]
        public void EmptyArguments_Span(string spanType)
        {
            string source = $$"""
                using System;
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>().Report();
                        EmptyArgs<int>().Report();
                    }
                    static T[] NoArgs<T>()
                    {
                        {{spanType}}<T> x = [];
                        return x.ToArray();
                    }
                    static T[] EmptyArgs<T>()
                    {
                        {{spanType}}<T> x = [with()];
                        return x.ToArray();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            string expectedIL = $$"""
                {
                  // Code size       16 (0x10)
                  .maxstack  1
                  .locals init (System.{{spanType}}<T> V_0) //x
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.{{spanType}}<T>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  call       "T[] System.{{spanType}}<T>.ToArray()"
                  IL_000f:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T>", expectedIL);
        }

        [Theory]
        [InlineData("ReadOnlySpan")]
        [InlineData("Span")]
        public void Arguments_Span(string spanType)
        {
            string source = $$"""
                using System;
                class Program
                {
                    static void F<T>(T t)
                    {
                        {{spanType}}<T> x =
                            [with(default), t];
                        {{spanType}}<T> y =
                            [t, with(default)];
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9276: Collection arguments are not supported for type 'ReadOnlySpan<T>'.
                //             [with(default), t];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.{spanType}<T>").WithLocation(7, 14),
                // (9,17): error CS9275: Collection argument element must be the first element.
                //             [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(9, 17),
                // (9,17): error CS9276: Collection arguments are not supported for type 'ReadOnlySpan<T>'.
                //             [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.{spanType}<T>").WithLocation(9, 17));
        }

        [Fact]
        public void EmptyArguments_List_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>().Report();
                        EmptyArgs<int>().Report();
                    }
                    static List<T> NoArgs<T>() => [];
                    static List<T> EmptyArgs<T>() => [with()];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            string expectedIL = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T>", expectedIL);
        }

        [Fact]
        public void EmptyArguments_List_02()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>(1, 2).Report();
                        EmptyArgs<int>(3, 4).Report();
                    }
                    static List<T> NoArgs<T>(T x, T y) => [x, y];
                    static List<T> EmptyArgs<T>(T x, T y) => [with(), x, y];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 2], [3, 4], "));
            verifier.VerifyDiagnostics();
            string expectedIL = """
                {
                  // Code size       57 (0x39)
                  .maxstack  3
                  .locals init (int V_0,
                                System.Span<T> V_1,
                                int V_2)
                  IL_0000:  ldc.i4.2
                  IL_0001:  stloc.0
                  IL_0002:  ldloc.0
                  IL_0003:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                  IL_0008:  dup
                  IL_0009:  ldloc.0
                  IL_000a:  call       "void System.Runtime.InteropServices.CollectionsMarshal.SetCount<T>(System.Collections.Generic.List<T>, int)"
                  IL_000f:  dup
                  IL_0010:  call       "System.Span<T> System.Runtime.InteropServices.CollectionsMarshal.AsSpan<T>(System.Collections.Generic.List<T>)"
                  IL_0015:  stloc.1
                  IL_0016:  ldc.i4.0
                  IL_0017:  stloc.2
                  IL_0018:  ldloca.s   V_1
                  IL_001a:  ldloc.2
                  IL_001b:  call       "ref T System.Span<T>.this[int].get"
                  IL_0020:  ldarg.0
                  IL_0021:  stobj      "T"
                  IL_0026:  ldloc.2
                  IL_0027:  ldc.i4.1
                  IL_0028:  add
                  IL_0029:  stloc.2
                  IL_002a:  ldloca.s   V_1
                  IL_002c:  ldloc.2
                  IL_002d:  call       "ref T System.Span<T>.this[int].get"
                  IL_0032:  ldarg.1
                  IL_0033:  stobj      "T"
                  IL_0038:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T>", expectedIL);
        }

        [Fact]
        public void Arguments_List()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        var l = F(1);
                        l.Report();
                        Console.WriteLine(l.Capacity);
                    }
                    static List<T> F<T>(T t) => [with(capacity: 2), t];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                expectedOutput: "[1], 2");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<T>(T)", """
                {
                  // Code size       14 (0xe)
                  .maxstack  3
                  IL_0000:  ldc.i4.2
                  IL_0001:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                  IL_0006:  dup
                  IL_0007:  ldarg.0
                  IL_0008:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                  IL_000d:  ret
                }
                """);
        }

        [Theory]
        [InlineData("IEnumerable")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlyList")]
        [InlineData("ICollection")]
        [InlineData("IList")]
        public void EmptyArguments_ArrayInterface(string interfaceType)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>().Report();
                        EmptyArgs<int>().Report();
                    }
                    static {{interfaceType}}<T> NoArgs<T>() => [];
                    static {{interfaceType}}<T> EmptyArgs<T>() => [with()];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            string expectedIL;
            if (interfaceType is "IEnumerable" or "IReadOnlyCollection" or "IReadOnlyList")
            {
                expectedIL = """
                    {
                      // Code size        6 (0x6)
                      .maxstack  1
                      IL_0000:  call       "T[] System.Array.Empty<T>()"
                      IL_0005:  ret
                    }
                    """;
            }
            else
            {
                expectedIL = """
                    {
                      // Code size        6 (0x6)
                      .maxstack  1
                      IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
                      IL_0005:  ret
                    }
                    """;
            }
            verifier.VerifyIL("Program.NoArgs<T>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T>", expectedIL);
        }

        [Theory]
        [InlineData("IEnumerable")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlyList")]
        [InlineData("ICollection")]
        [InlineData("IList")]
        public void Arguments_ArrayInterface(string interfaceType)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void F<T>(T t)
                    {
                        {{interfaceType}}<T> i;
                        i = [with(default), t];
                        i = [t, with(default)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9276: Collection arguments are not supported for type 'IEnumerable<T>'.
                //         i = [with(default), t];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.Collections.Generic.{interfaceType}<T>").WithLocation(7, 14),
                // (8,17): error CS9275: Collection argument element must be the first element.
                //         i = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17),
                // (8,17): error CS9276: Collection arguments are not supported for type 'IEnumerable<T>'.
                //         i = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.Collections.Generic.{interfaceType}<T>").WithLocation(8, 17));

            // Collection arguments do not affect convertibility.
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
            Assert.Equal(2, collections.Length);
            VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: $"System.Collections.Generic.{interfaceType}<T>", ConversionKind.CollectionExpression);
            VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: $"System.Collections.Generic.{interfaceType}<T>", ConversionKind.CollectionExpression);
        }

        [Theory]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void EmptyArguments_DictionaryInterface(string interfaceType)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        NoArgs<string, int>().Report();
                        EmptyArgs<string, int>().Report();
                    }
                    static {{interfaceType}}<K, V> NoArgs<K, V>() => [];
                    static {{interfaceType}}<K, V> EmptyArgs<K, V>() => [with()];
                }
                """;
            var verifier = CompileAndVerify(
                [source, s_collectionExtensions],
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            string expectedIL = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<K, V>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<K, V>", expectedIL);
        }

        [Theory]
        [InlineData("IDictionary")]
        [InlineData("IReadOnlyDictionary")]
        public void Arguments_DictionaryInterface(string interfaceType)
        {
            string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void F<K, V>(K k, V v)
                    {
                        {{interfaceType}}<K, V> i;
                        i = [with(default), k:v];
                        i = [k:v, with(default)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9276: Collection arguments are not supported for type 'IDictionary<K, V>'.
                //         i = [with(default), k:v];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.Collections.Generic.{interfaceType}<K, V>").WithLocation(7, 14),
                // (8,19): error CS9275: Collection argument element must be the first element.
                //         i = [k:v, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 19),
                // (8,19): error CS9276: Collection arguments are not supported for type 'IDictionary<K, V>'.
                //         i = [k:v, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.Collections.Generic.{interfaceType}<K, V>").WithLocation(8, 19));
        }

        [Fact]
        public void CollectionInitializer_MultipleConstructors()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T Arg;
                    public MyCollection() { }
                    public MyCollection(T arg) { Arg = arg; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((EmptyArgs<int>().Arg, NonEmptyArgs(2).Arg));
                    }
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                expectedOutput: "(0, 2)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>()", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "MyCollection<T>..ctor()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.NonEmptyArgs<T>(T)", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "MyCollection<T>..ctor(T)"
                  IL_0006:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializer_NoParameterlessConstructor()
        {
            string source = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T Arg;
                    public MyCollection(T arg) { Arg = arg; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class Program
                {
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,46): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //     static MyCollection<T> EmptyArgs<T>() => [with()];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with()]").WithLocation(13, 46),
                // (14,52): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with(t)]").WithLocation(14, 52));
        }

        [Fact]
        public void CollectionInitializer_OptionalParameter()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T Arg;
                    public MyCollection(T arg = default) { Arg = arg; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((EmptyArgs<int>().Arg, NonEmptyArgs(2).Arg));
                    }
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                expectedOutput: "(0, 2)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>()", """
                {
                  // Code size       15 (0xf)
                  .maxstack  1
                  .locals init (T V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "T"
                  IL_0008:  ldloc.0
                  IL_0009:  newobj     "MyCollection<T>..ctor(T)"
                  IL_000e:  ret
                }
                """);
            verifier.VerifyIL("Program.NonEmptyArgs<T>(T)", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "MyCollection<T>..ctor(T)"
                  IL_0006:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializer_ParamsParameter()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T[] Args;
                    public MyCollection(params T[] args) { Args = args; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        EmptyArgs<int>().Args.Report();
                        OneArg(1).Args.Report();
                        TwoArgs(2, 3).Args.Report();
                        MultipleArgs([4, 5]).Args.Report();
                    }
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> OneArg<T>(T t) => [with(t)];
                    static MyCollection<T> TwoArgs<T>(T x, T y) => [with(x, y)];
                    static MyCollection<T> MultipleArgs<T>(T[] args) => [with(args)];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                expectedOutput: "[], [1], [2, 3], [4, 5], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>()", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  call       "T[] System.Array.Empty<T>()"
                  IL_0005:  newobj     "MyCollection<T>..ctor(params T[])"
                  IL_000a:  ret
                }
                """);
            verifier.VerifyIL("Program.OneArg<T>(T)", """
                {
                  // Code size       20 (0x14)
                  .maxstack  4
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "T"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldarg.0
                  IL_0009:  stelem     "T"
                  IL_000e:  newobj     "MyCollection<T>..ctor(params T[])"
                  IL_0013:  ret
                }
                """);
            verifier.VerifyIL("Program.TwoArgs<T>(T, T)", """
                {
                  // Code size       28 (0x1c)
                  .maxstack  4
                  IL_0000:  ldc.i4.2
                  IL_0001:  newarr     "T"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldarg.0
                  IL_0009:  stelem     "T"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.1
                  IL_0010:  ldarg.1
                  IL_0011:  stelem     "T"
                  IL_0016:  newobj     "MyCollection<T>..ctor(params T[])"
                  IL_001b:  ret
                }
                """);
            verifier.VerifyIL("Program.MultipleArgs<T>(T[])", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "MyCollection<T>..ctor(params T[])"
                  IL_0006:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T Arg;
                    private readonly List<T> _items;
                    public MyCollection(T arg, ReadOnlySpan<T> items) { Arg = arg; _items = new(items.ToArray()); }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(default, items);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg) => new(arg, items);
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = EmptyArgs(1);
                        Console.Write("{0}, ", c.Arg);
                        c.Report();
                        c = NonEmptyArgs<int>(2);
                        Console.Write("{0}, ", c.Arg);
                        c.Report();
                    }
                    static MyCollection<T> EmptyArgs<T>(T t) => [with(), t];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t), t];
                }
                """;
            // PROTOTYPE: When collection arguments are supported for collection builder
            // types, use CompileAndVerify() and check expectedOutput and the code
            // generated for Program.EmptyArgs<T>(T) and Program.NonEmptyArgs<T>(T)
            var comp = CreateCompilation(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (15,53): error CS9276: Collection arguments are not supported for type 'MyCollection<T>'.
                //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t), t];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("MyCollection<T>").WithLocation(15, 53));
        }

        [Fact]
        public void Empty_TypeParameter()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                interface IAdd<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                struct MyCollection<T> : IAdd<T>
                {
                    private List<T> _list;
                    void IAdd<T>.Add(T t) { GetList().Add(t); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetList().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetList().GetEnumerator();
                    private List<T> GetList() => _list ??= new();
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int, MyCollection<int>>().Report();
                        EmptyArgs<int, MyCollection<int>>().Report();
                    }
                    static U NoArgs<T, U>()
                        where U : IAdd<T>, new()
                    {
                        return [];
                    }
                    static U EmptyArgs<T, U>()
                        where U : IAdd<T>, new()
                    {
                        return [with()];
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            string expectedIL = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "U System.Activator.CreateInstance<U>()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T, U>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T, U>", expectedIL);

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int, MyCollection<int>>().Report();
                        EmptyArgs<int, MyCollection<int>>().Report();
                    }
                    static U NoArgs<T, U>()
                        where U : struct, IAdd<T>
                    {
                        return [];
                    }
                    static U EmptyArgs<T, U>()
                        where U : struct, IAdd<T>
                    {
                        return [with()];
                    }
                }
                """;
            verifier = CompileAndVerify(
                [sourceA, sourceB2, s_collectionExtensions],
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [], "));
            verifier.VerifyDiagnostics();
            expectedIL = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  call       "U System.Activator.CreateInstance<U>()"
                  IL_0005:  ret
                }
                """;
            verifier.VerifyIL("Program.NoArgs<T, U>", expectedIL);
            verifier.VerifyIL("Program.EmptyArgs<T, U>", expectedIL);
        }

        [Fact]
        public void Arguments_TypeParameter()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                interface IAdd<T> : IEnumerable<T>
                {
                    void Add(T t);
                }
                """;
            string sourceB = """
                class Program
                {
                    static void NonEmptyArgsNew<T, U>(T t)
                        where U : IAdd<T>, new()
                    {
                        U x;
                        x = [with(t), t];
                        x = [t, with(t)];
                    }
                    static void NonEmptyArgsStruct<T, U>(T t)
                        where U : struct, IAdd<T>
                    {
                        U y;
                        y = [with(t), t];
                        y = [t, with(t)];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
                //         x = [with(t), t];
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "[with(t), t]").WithArguments("U").WithLocation(7, 13),
                // (8,13): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
                //         x = [t, with(t)];
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "[t, with(t)]").WithArguments("U").WithLocation(8, 13),
                // (8,17): error CS9275: Collection argument element must be the first element.
                //         x = [t, with(t)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17),
                // (14,13): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
                //         y = [with(t), t];
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "[with(t), t]").WithArguments("U").WithLocation(14, 13),
                // (15,13): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
                //         y = [t, with(t)];
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "[t, with(t)]").WithArguments("U").WithLocation(15, 13),
                // (15,17): error CS9275: Collection argument element must be the first element.
                //         y = [t, with(t)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(15, 17));
        }

        [Fact]
        public void UnrecognizedType()
        {
            string source = """
                class Program
                {
                    static A EmptyArgs() => [with()];
                    static B NonEmptyArgs() => [with(default)];
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //     static A EmptyArgs() => [with()];
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(3, 12),
                // (4,12): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //     static B NonEmptyArgs() => [with(default)];
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(4, 12));
        }

        [Fact]
        public void EvaluationOrder_01()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class A
                {
                    private int _i;
                    private A(int i) { _i = i; }
                    public static implicit operator A(int i)
                    {
                        Console.WriteLine("{0} -> A", i);
                        return new(i);
                    }
                    public override string ToString() => _i.ToString();
                }
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection(A x = null, A y = null) { Console.WriteLine("MyCollection({0}, {1})", x, y); }
                    public void Add(T t) { Console.WriteLine("Add({0})", t); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        MyCollection<A> c;
                        c = [with(y: Identity(1), x: Identity(2)), Identity(3), Identity(4)];
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                expectedOutput: """
                    1
                    1 -> A
                    2
                    2 -> A
                    MyCollection(2, 1)
                    3
                    3 -> A
                    Add(3)
                    4
                    4 -> A
                    Add(4)
                    """);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.Main()", """
                {
                  // Code size       65 (0x41)
                  .maxstack  3
                  .locals init (A V_0)
                  IL_0000:  ldc.i4.1
                  IL_0001:  call       "int Program.Identity<int>(int)"
                  IL_0006:  call       "A A.op_Implicit(int)"
                  IL_000b:  stloc.0
                  IL_000c:  ldc.i4.2
                  IL_000d:  call       "int Program.Identity<int>(int)"
                  IL_0012:  call       "A A.op_Implicit(int)"
                  IL_0017:  ldloc.0
                  IL_0018:  newobj     "MyCollection<A>..ctor(A, A)"
                  IL_001d:  dup
                  IL_001e:  ldc.i4.3
                  IL_001f:  call       "int Program.Identity<int>(int)"
                  IL_0024:  call       "A A.op_Implicit(int)"
                  IL_0029:  callvirt   "void MyCollection<A>.Add(A)"
                  IL_002e:  dup
                  IL_002f:  ldc.i4.4
                  IL_0030:  call       "int Program.Identity<int>(int)"
                  IL_0035:  call       "A A.op_Implicit(int)"
                  IL_003a:  callvirt   "void MyCollection<A>.Add(A)"
                  IL_003f:  pop
                  IL_0040:  ret
                }
                """);
        }

        [Fact]
        public void Arglist()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection : IEnumerable
                {
                    public readonly List<int> Args;
                    public MyCollection() { Args = new(); }
                    public MyCollection(__arglist) { Args = GetArgs(0, new ArgIterator(__arglist)); }
                    public MyCollection(int x, __arglist) { Args = GetArgs(x, new ArgIterator(__arglist)); }
                    private static List<int> GetArgs(int x, ArgIterator iterator)
                    {
                        var args = new List<int>();
                        args.Add(x);
                        while (iterator.GetRemainingCount() > 0)
                            args.Add(__refvalue(iterator.GetNextArg(), int));
                        return args;
                    }
                    public void Add(object o) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        F1().Args.Report();
                        F2().Args.Report();
                        F3(1, 2).Args.Report();
                        F4(3, 4).Args.Report();
                    }
                    static MyCollection F1() => [with()];
                    static MyCollection F2() => [with(__arglist())];
                    static MyCollection F3(int x, int y) => [with(__arglist(x, y))];
                    static MyCollection F4(int x, int y) => [with(x, __arglist(y))];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.NetFramework,
                verify: Verification.FailsILVerify,
                expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? null : "[], [0], [0, 1, 2], [3, 4], ");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F1", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "MyCollection..ctor()"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.F2", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "MyCollection..ctor(__arglist)"
                  IL_0005:  ret
                }
                """);
            verifier.VerifyIL("Program.F3", """
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  newobj     "MyCollection..ctor(__arglist) with __arglist( int, int)"
                  IL_0007:  ret
                }
                """);
            verifier.VerifyIL("Program.F4", """
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  newobj     "MyCollection..ctor(int, __arglist) with __arglist( int)"
                  IL_0007:  ret
                }
                """);
        }

        [Fact]
        public void Arglist_NoParameterlessConstructor()
        {
            string sourceA = """
                using System;
                using System.Collections;
                class MyCollection : IEnumerable
                {
                    public MyCollection(__arglist) { }
                    public void Add(object o) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        MyCollection c;
                        c = [];
                        c = [with()];
                        c = [with(__arglist())];
                        c = [with(__arglist(0))];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //         c = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[]").WithLocation(7, 13),
                // (8,13): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //         c = [with()];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with()]").WithLocation(8, 13),
                // (9,13): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //         c = [with(__arglist())];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with(__arglist())]").WithLocation(9, 13),
                // (10,13): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                //         c = [with(__arglist(0))];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with(__arglist(0))]").WithLocation(10, 13));
        }

        [Fact]
        public void DynamicArguments_01()
        {
            string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<object> l;
                        l = [with(), (dynamic)2];
                        l = [with(capacity: 1)];
                        l = [with(capacity: (dynamic)1)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,29): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         l = [with(capacity: (dynamic)1)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)1").WithLocation(9, 29));
        }

        [Fact]
        public void DynamicArguments_02()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection(object x = null, object y = null) { }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [with(), (dynamic)3];
                        c = [with(1)];
                        c = [with(y: "2")];
                        c = [with(1, "2"), (dynamic)3];
                        c = [with((dynamic)1)];
                        c = [with(y: (dynamic)"2")];
                        c = [3, with(1, (dynamic)"2")];
                        c = [with((dynamic)1, (dynamic)"2"), 3];
                        c = [with(x => { })];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (10,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with((dynamic)1)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)1").WithLocation(10, 19),
                // (11,22): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with(y: (dynamic)"2")];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, @"(dynamic)""2""").WithLocation(11, 22),
                // (12,17): error CS9275: Collection argument element must be the first element.
                //         c = [3, with(1, (dynamic)"2")];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(12, 17),
                // (12,25): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [3, with(1, (dynamic)"2")];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, @"(dynamic)""2""").WithLocation(12, 25),
                // (13,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with((dynamic)1, (dynamic)"2"), 3];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)1").WithLocation(13, 19),
                // (14,21): error CS8917: The delegate type could not be inferred.
                //         c = [with(x => { })];
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(14, 21));
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("in ")]
        [InlineData("out")]
        public void DynamicArguments_03(string refKind)
        {
            string source = $$"""
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection() { }
                    public MyCollection({{refKind}} object obj) { throw null; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class Program
                {
                    static void Main()
                    {
                        object o = null;
                        dynamic d = o;
                        MyCollection<object> c;
                        c = [with({{refKind}} o)];
                        c = [with({{refKind}} d)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (19,23): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with(in  d)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(19, 23));
        }

        [Fact]
        public void DynamicArguments_04()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection : IEnumerable
                {
                    public MyCollection(dynamic d = null) { }
                    public void Add(object o) { }
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        object o = null;
                        dynamic d = o;
                        MyCollection c;
                        c = [with()];
                        c = [with(null)];
                        c = [with(default)];
                        c = [with(0)];
                        c = [with((dynamic)null)];
                        c = [with((dynamic)0)];
                        c = [with(o)];
                        c = [with(d)];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (12,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with((dynamic)null)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)null").WithLocation(12, 19),
                // (13,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with((dynamic)0)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)0").WithLocation(13, 19),
                // (15,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         c = [with(d)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(15, 19));
        }

        [Fact]
        public void DynamicArguments_05()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        A a;
                        a = [with(null), (dynamic)null];
                        a = [with((dynamic)null), null];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         A a;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(5, 9),
                // (7,19): error CS9277: Collection arguments cannot be dynamic; compile-time binding is required.
                //         a = [with((dynamic)null), null];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)null").WithLocation(7, 19));
        }

        [Fact]
        public void ParamsCycle_ParamsConstructorOnly()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    public MyCollection(params MyCollection<T> other) { _list = new(other); }
                    public void Add(T t) { _list.Add(t); }
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [];
                c = [with()];
                c = [with(null)];
                c = [with(1)];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(2, 5),
                // (3,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [with()];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[with()]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(3, 5),
                // (5,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [with(1)];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[with(1)]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(5, 5));
        }

        [Fact]
        public void ParamsCycle_MultipleConstructors()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    public MyCollection() { _list = new(); }
                    public MyCollection(params MyCollection<T> other) { _list = new(other); }
                    public void Add(T t) { _list.Add(t); }
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [];
                c = [with()];
                c = [with(null)];
                c = [with(1)];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (5,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection()'.
                // c = [with(1)];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[with(1)]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection()").WithLocation(5, 5));
        }

        [Fact]
        public void ParamsCycle_PrivateParameterlessConstructor()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    private MyCollection() { }
                    public MyCollection(params MyCollection<T> other) { _list = new(other); }
                    public void Add(T t) { _list.Add(t); }
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [];
                c = [with()];
                c = [with(null)];
                c = [with(1)];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(2, 5),
                // (3,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [with()];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[with()]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(3, 5),
                // (5,5): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                // c = [with(1)];
                Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "[with(1)]").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(5, 5),
                // (9,25): error CS9224: Method 'MyCollection<T>.MyCollection()' cannot be less visible than the member with params collection 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
                //     public MyCollection(params MyCollection<T> other) { _list = new(other); }
                Diagnostic(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, "params MyCollection<T> other").WithArguments("MyCollection<T>.MyCollection()", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(9, 25));
        }

        [Fact]
        public void ParamsCycle_NoParameterlessConstructor()
        {
            string sourceA = """
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                    public MyCollection(T x, params MyCollection<T> y)
                    {
                        _list = new();
                        _list.Add(x);
                        _list.AddRange(y);
                    }
                    public void Add(T t) { _list.Add(t); }
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [];
                c = [with()];
                c = [with(1)];
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                // c = [];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[]").WithLocation(2, 5),
                // (3,5): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                // c = [with()];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with()]").WithLocation(3, 5),
                // (4,5): error CS9214: Collection expression type must have an applicable constructor that can be called with no arguments.
                // c = [with(1)];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingConstructor, "[with(1)]").WithLocation(4, 5),
                // (8,30): error CS9228: Non-array params collection type must have an applicable constructor that can be called with no arguments.
                //     public MyCollection(T x, params MyCollection<T> y)
                Diagnostic(ErrorCode.ERR_ParamsCollectionMissingConstructor, "params MyCollection<T> y").WithLocation(8, 30));
        }
    }
}
