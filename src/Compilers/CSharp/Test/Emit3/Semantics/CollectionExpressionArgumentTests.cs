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

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersion_04(LanguageVersion languageVersion)
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) => default;
                }
                """;
            string sourceB = """
                MyCollection<int> c = [
                    with(),
                    with(arg: 0),
                    with(unknown: 1)];
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                targetFramework: TargetFramework.Net80);
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (2,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     with(),
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 5),
                    // (3,5): error CS9275: Collection argument element must be the first element.
                    //     with(arg: 0),
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(3, 5),
                    // (3,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     with(arg: 0),
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(3, 5),
                    // (4,5): error CS9275: Collection argument element must be the first element.
                    //     with(unknown: 1)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(4, 5),
                    // (4,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     with(unknown: 1)];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(4, 5),
                    // (4,10): error CS1739: The best overload for 'Create' does not have a parameter named 'unknown'
                    //     with(unknown: 1)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "unknown").WithArguments("Create", "unknown").WithLocation(4, 10));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,5): error CS9275: Collection argument element must be the first element.
                    //     with(arg: 0),
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(3, 5),
                    // (4,5): error CS9275: Collection argument element must be the first element.
                    //     with(unknown: 1)];
                    Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(4, 5),
                    // (4,10): error CS1739: The best overload for 'Create' does not have a parameter named 'unknown'
                    //     with(unknown: 1)];
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "unknown").WithArguments("Create", "unknown").WithLocation(4, 10));
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
        public void CollectionInitializer_ObsoleteConstructor_01()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection() { }
                    [Obsolete]
                    public MyCollection(T arg) { }
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
                        c = [with()];
                        c = [with(default)];
                    }
                    static void F<T>(params MyCollection<T> c) { }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (7,13): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
                //         c = [with(default)];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[with(default)]").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(7, 13));
        }

        [Fact]
        public void CollectionInitializer_ObsoleteConstructor_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    [Obsolete]
                    public MyCollection(T arg = default) { }
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
                        c = [with()];
                        c = [with(default)];
                    }
                    static void F<T>(params MyCollection<T> c) { }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB]);
            comp.VerifyEmitDiagnostics(
                // (6,13): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
                //         c = [with()];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[with()]").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(6, 13),
                // (7,13): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
                //         c = [with(default)];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[with(default)]").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(7, 13),
                // (9,22): warning CS0612: 'MyCollection<T>.MyCollection(T)' is obsolete
                //     static void F<T>(params MyCollection<T> c) { }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "params MyCollection<T> c").WithArguments("MyCollection<T>.MyCollection(T)").WithLocation(9, 22));
        }

        [Fact]
        public void CollectionBuilder_MultipleConstructors()
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
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("0, [1], 2, [2], "));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>(T)", """
                {
                  // Code size       15 (0xf)
                  .maxstack  1
                  .locals init (T V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldloca.s   V_0
                  IL_0004:  newobj     "System.ReadOnlySpan<T>..ctor(ref readonly T)"
                  IL_0009:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>)"
                  IL_000e:  ret
                }
                """);
            verifier.VerifyIL("Program.NonEmptyArgs<T>(T)", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  .locals init (T V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  stloc.0
                  IL_0002:  ldloca.s   V_0
                  IL_0004:  newobj     "System.ReadOnlySpan<T>..ctor(ref readonly T)"
                  IL_0009:  ldarg.0
                  IL_000a:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)"
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_NoParameterlessConstructor()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg) => default;
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (4,46): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //     static MyCollection<T> EmptyArgs<T>() => [with()];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(4, 46),
                // (4,47): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //     static MyCollection<T> EmptyArgs<T>() => [with()];
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with()").WithArguments("arg", "MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(4, 47),
                // (5,52): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(t)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(5, 52),
                // (6,38): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //     static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 38));
        }

        [Fact]
        public void CollectionBuilder_OptionalParameter()
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
                    public MyCollection(T arg, ReadOnlySpan<T> items) { Arg = arg; }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) => new(arg, items);
                }
                """;
            string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(EmptyArgs<int>().Arg);
                        Console.WriteLine(NonEmptyArgs(2).Arg);
                        Console.WriteLine(Params(3, 4).Arg);
                    }
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("""
                    0
                    2
                    0
                    """));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>()", """
                {
                  // Code size       24 (0x18)
                  .maxstack  2
                  .locals init (System.ReadOnlySpan<T> V_0,
                                T V_1)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  ldloca.s   V_1
                  IL_000b:  initobj    "T"
                  IL_0011:  ldloc.1
                  IL_0012:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)"
                  IL_0017:  ret
                }
                """);
            verifier.VerifyIL("Program.NonEmptyArgs<T>(T)", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  .locals init (System.ReadOnlySpan<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  ldarg.0
                  IL_000a:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)"
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_ParamsParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    public readonly T[] Args;
                    public MyCollection(ReadOnlySpan<T> items, T[] args) { Args = args; }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, params T[] args) => new(items, args);
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
                        Params(6).Args.Report();
                    }
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> OneArg<T>(T t) => [with(t)];
                    static MyCollection<T> TwoArgs<T>(T x, T y) => [with(x, y)];
                    static MyCollection<T> MultipleArgs<T>(T[] args) => [with(args)];
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1], [2, 3], [4, 5], [], "));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.EmptyArgs<T>()", """
                {
                  // Code size       20 (0x14)
                  .maxstack  2
                  .locals init (System.ReadOnlySpan<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  call       "T[] System.Array.Empty<T>()"
                  IL_000e:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])"
                  IL_0013:  ret
                }
                """);
            verifier.VerifyIL("Program.OneArg<T>(T)", """
                {
                  // Code size       29 (0x1d)
                  .maxstack  5
                  .locals init (System.ReadOnlySpan<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  ldc.i4.1
                  IL_000a:  newarr     "T"
                  IL_000f:  dup
                  IL_0010:  ldc.i4.0
                  IL_0011:  ldarg.0
                  IL_0012:  stelem     "T"
                  IL_0017:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])"
                  IL_001c:  ret
                }
                """);
            verifier.VerifyIL("Program.TwoArgs<T>(T, T)", """
                {
                  // Code size       37 (0x25)
                  .maxstack  5
                  .locals init (System.ReadOnlySpan<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  ldc.i4.2
                  IL_000a:  newarr     "T"
                  IL_000f:  dup
                  IL_0010:  ldc.i4.0
                  IL_0011:  ldarg.0
                  IL_0012:  stelem     "T"
                  IL_0017:  dup
                  IL_0018:  ldc.i4.1
                  IL_0019:  ldarg.1
                  IL_001a:  stelem     "T"
                  IL_001f:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])"
                  IL_0024:  ret
                }
                """);
            verifier.VerifyIL("Program.MultipleArgs<T>(T[])", """
                {
                  // Code size       16 (0x10)
                  .maxstack  2
                  .locals init (System.ReadOnlySpan<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "System.ReadOnlySpan<T>"
                  IL_0008:  ldloc.0
                  IL_0009:  ldarg.0
                  IL_000a:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])"
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_ImplicitParameter_Optional()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items) { _list = new(items.ToArray()); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items = default) => new(items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c.Report();
                        c = [1, 2, 3];
                        c.Report();
                        c = [with(), 4];
                        c.Report();
                        F(5, 6);
                    }
                    static void F<T>(params MyCollection<T> c)
                    {
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], [4], [5, 6], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [with(1)];
                        c = [with(2), 3];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS1501: No overload for method 'Create' takes 2 arguments
                //         c = [with(1)];
                Diagnostic(ErrorCode.ERR_BadArgCount, "with(1)").WithArguments("Create", "2").WithLocation(6, 14),
                // (7,14): error CS1501: No overload for method 'Create' takes 2 arguments
                //         c = [with(2), 3];
                Diagnostic(ErrorCode.ERR_BadArgCount, "with(2)").WithArguments("Create", "2").WithLocation(7, 14));
        }

        [Fact]
        public void CollectionBuilder_ImplicitParameter_Params()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items) { _list = new(items.ToArray()); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(params ReadOnlySpan<T> items) => new(items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c.Report();
                        c = [1, 2, 3];
                        c.Report();
                        c = [with(), 4];
                        c.Report();
                        F(5, 6);
                    }
                    static void F<T>(params MyCollection<T> c)
                    {
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1, 2, 3], [4], [5, 6], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [with(1)];
                        c = [with(2), 3];
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'int'
                //         c = [with(1)];
                Diagnostic(ErrorCode.ERR_BadArgType, "[with(1)]").WithArguments("1", "System.ReadOnlySpan<int>", "int").WithLocation(6, 13),
                // (7,13): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'int'
                //         c = [with(2), 3];
                Diagnostic(ErrorCode.ERR_BadArgType, "[with(2), 3]").WithArguments("1", "System.ReadOnlySpan<int>", "int").WithLocation(7, 13));
        }

        [Fact]
        public void CollectionBuilder_MultipleConstructors_NoArguments()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection(ReadOnlySpan<T> items)
                    {
                        _items = new(items.ToArray());
                    }
                    public MyCollection(T arg, ReadOnlySpan<T> items)
                    {
                        _items = new();
                        _items.Add(arg);
                        _items.AddRange(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) where T : class => new(items);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) where T : struct => new(arg, items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> x = [1, 2, 3];
                        x.Report();
                        MyCollection<object> y = [4, 5];
                        y.Report();
                        Params<int>(1, 2, 3).Report();
                        Params<object>(4, 5).Report();
                        ThreeItems<int>(1, 2, 3).Report();
                        TwoItems<object>(4, 5).Report();
                    }
                    static MyCollection<T> TwoItems<T>(T x, T y) where T : class => Params(x, y);
                    static MyCollection<T> ThreeItems<T>(T x, T y, T z) where T : struct => Params(x, y, z);
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
            // C#7.3 feature ImprovedOverloadCandidates drops candidates with constraint violations
            // (see OverloadResolution.RemoveConstraintViolations()) which allows constructing
            // MyCollection<int> and MyCollection<object> with different factory methods.
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0, 1, 2, 3], [4, 5], [0, 1, 2, 3], [4, 5], [0, 1, 2, 3], [4, 5], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        TwoItems(1, 2);
                    }
                    static MyCollection<T> TwoItems<T>(T x, T y) => Params(x, y);
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (7,53): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
                //     static MyCollection<T> TwoItems<T>(T x, T y) => Params(x, y);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "Params(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(7, 53));
        }

        [Fact]
        public void CollectionBuilder_NamedParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T x, T y)
                    {
                        _list = new();
                        _list.Add(x);
                        _list.Add(y);
                        _list.AddRange(items.ToArray());
                    }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T x = default, T y = default) => new(items, x, y);
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [with(x: 1), 2, 3];
                c.Report();
                c = [with(y: 4), 5];
                c.Report();
                c = [with(y: 6, x: 7), 8];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 0, 2, 3], [0, 4, 5], [7, 6, 8], "));
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void CollectionBuilder_RefParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T arg) { _list = new(items.ToArray()); _list.Add(arg); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, ref T x) => new(items, x);
                }
                """;

            string sourceB1 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref int r = ref x;
                c = [with(ref x)];
                c.Report();
                x = 2;
                c = [with(ref r)];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1], [2], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref readonly int ro = ref x;
                c = [with(0)];
                c = [with(x)];
                c = [with(in x)];
                c = [with(ref ro)];
                c = [with(out x)];
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (5,11): error CS1620: Argument 2 must be passed with the 'ref' keyword
                // c = [with(0)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "0").WithArguments("2", "ref").WithLocation(5, 11),
                // (6,11): error CS1620: Argument 2 must be passed with the 'ref' keyword
                // c = [with(x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "ref").WithLocation(6, 11),
                // (7,14): error CS1620: Argument 2 must be passed with the 'ref' keyword
                // c = [with(in x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "ref").WithLocation(7, 14),
                // (8,15): error CS1510: A ref or out value must be an assignable variable
                // c = [with(ref ro)];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(8, 15),
                // (9,15): error CS1620: Argument 2 must be passed with the 'ref' keyword
                // c = [with(out x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "ref").WithLocation(9, 15));
        }

        [Fact]
        public void CollectionBuilder_RefReadonlyParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T arg) { _list = new(items.ToArray()); _list.Add(arg); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, ref readonly T x) => new(items, x);
                }
                """;

            string sourceB1 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref int r = ref x;
                ref readonly int ro = ref x;
                c = [with(0)];
                c.Report();
                c = [with(x)];
                c.Report();
                x = 2;
                c = [with(ref x)];
                c.Report();
                x = 3;
                c = [with(ref r)];
                c.Report();
                x = 4;
                c = [with(in ro)];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0], [1], [2], [3], [4], "));
            verifier.VerifyDiagnostics(
                // (6,11): warning CS9193: Argument 2 should be a variable because it is passed to a 'ref readonly' parameter
                // c = [with(0)];
                Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "0").WithArguments("2").WithLocation(6, 11),
                // (8,11): warning CS9192: Argument 2 should be passed with 'ref' or 'in' keyword
                // c = [with(x)];
                Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("2").WithLocation(8, 11));

            string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref readonly int ro = ref x;
                c = [with(in x)];
                c = [with(ref ro)];
                c = [with(out x)];
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,15): error CS1510: A ref or out value must be an assignable variable
                // c = [with(ref ro)];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(6, 15),
                // (7,15): error CS1615: Argument 2 may not be passed with the 'out' keyword
                // c = [with(out x)];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("2", "out").WithLocation(7, 15));
        }

        [Fact]
        public void CollectionBuilder_InParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T arg) { _list = new(items.ToArray()); _list.Add(arg); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, in T x) => new(items, x);
                }
                """;

            string sourceB1 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref int r = ref x;
                ref readonly int ro = ref x;
                c = [with(0)];
                c.Report();
                c = [with(x)];
                c.Report();
                x = 2;
                c = [with(ref x)];
                c.Report();
                x = 3;
                c = [with(in x)];
                c.Report();
                x = 4;
                c = [with(in r)];
                c.Report();
                x = 5;
                c = [with(in ro)];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0], [1], [2], [3], [4], [5], "));
            verifier.VerifyDiagnostics(
                // (11,15): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
                // c = [with(ref x)];
                Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("2").WithLocation(11, 15));

            string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref readonly int ro = ref x;
                c = [with(ref ro)];
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (5,15): error CS1510: A ref or out value must be an assignable variable
                // c = [with(ref ro)];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(5, 15));
        }

        [Fact]
        public void CollectionBuilder_OutParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T arg) { _list = new(items.ToArray()); _list.Add(arg); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, out T x) { x = default; return new(items, x); }
                }
                """;

            string sourceB1 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref int r = ref x;
                c = [with(out x)];
                c.Report();
                x = 2;
                c = [with(out r), 3];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0], [3, 0], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                c = [with(1)];
                c = [with(x)];
                c = [with(ref x)];
                c = [with(in x)];
                """;
            var comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (4,11): error CS1620: Argument 2 must be passed with the 'out' keyword
                // c = [with(1)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "1").WithArguments("2", "out").WithLocation(4, 11),
                // (5,11): error CS1620: Argument 2 must be passed with the 'out' keyword
                // c = [with(x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "out").WithLocation(5, 11),
                // (6,15): error CS1620: Argument 2 must be passed with the 'out' keyword
                // c = [with(ref x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "out").WithLocation(6, 15),
                // (7,14): error CS1620: Argument 2 must be passed with the 'out' keyword
                // c = [with(in x)];
                Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("2", "out").WithLocation(7, 14));
        }

        [Fact]
        public void CollectionBuilder_RefParameter_Overloads()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T x, T y)
                    {
                        _list = new();
                        _list.Add(x);
                        _list.Add(y);
                        _list.AddRange(items.ToArray());
                    }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, in T x) => new(items, x, default);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T x, ref T y) => new(items, x, y);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, out T x, T y) { x = default; return new(items, x, y); }
                }
                """;
            string sourceB = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                int y = 2;
                c = [with(in x)];
                c.Report();
                c = [with(1), 3];
                c.Report();
                c = [with(x, ref y)];
                c.Report();
                c = [with(out x, y), 3];
                c.Report();
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[1, 0], [1, 0, 3], [1, 2], [0, 2, 3], "));
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void CollectionBuilder_ReferenceImplicitParameter()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    internal MyCollection(ReadOnlySpan<T> items, T arg) { _list = new(items.ToArray()); _list.Add(arg); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) => new(items, arg);
                }
                """;
            string sourceB = """
                MyCollection<int> c;
                c = [with(items: default)];
                c = [with(items: default, 1)];
                c = [with(items: default, arg: 2)];
                c = [with(3, items: default)];
                c = [with(arg: 4, items: default)];
                c = [with(default, 5)];
                c = [with(default, arg: 6)];
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,11): error CS1744: Named argument 'items' specifies a parameter for which a positional argument has already been given
                // c = [with(items: default)];
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "items").WithArguments("items").WithLocation(2, 11),
                // (3,11): error CS8323: Named argument 'items' is used out-of-position but is followed by an unnamed argument
                // c = [with(items: default, 1)];
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "items").WithArguments("items").WithLocation(3, 11),
                // (4,11): error CS1744: Named argument 'items' specifies a parameter for which a positional argument has already been given
                // c = [with(items: default, arg: 2)];
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "items").WithArguments("items").WithLocation(4, 11),
                // (5,14): error CS1744: Named argument 'items' specifies a parameter for which a positional argument has already been given
                // c = [with(3, items: default)];
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "items").WithArguments("items").WithLocation(5, 14),
                // (6,19): error CS1744: Named argument 'items' specifies a parameter for which a positional argument has already been given
                // c = [with(arg: 4, items: default)];
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "items").WithArguments("items").WithLocation(6, 19),
                // (7,6): error CS1501: No overload for method 'Create' takes 3 arguments
                // c = [with(default, 5)];
                Diagnostic(ErrorCode.ERR_BadArgCount, "with(default, 5)").WithArguments("Create", "3").WithLocation(7, 6),
                // (8,20): error CS1744: Named argument 'arg' specifies a parameter for which a positional argument has already been given
                // c = [with(default, arg: 6)];
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "arg").WithArguments("arg").WithLocation(8, 20));
        }

        [Fact]
        public void CollectionBuilder_SpreadElement_BoxingConversion()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                interface IMyCollection<T> : IEnumerable<T>
                {
                }
                class MyCollectionBuilder
                {
                    public struct MyCollection<T> : IMyCollection<T>
                    {
                        private readonly List<T> _list;
                        public MyCollection(ReadOnlySpan<T> items, T arg)
                        {
                            _list = new(items.ToArray());
                            _list.Add(arg);
                        }
                        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => throw null;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg) => new(items, arg);
                }
                """;
            string sourceB = """
                #nullable enable
                using System;
                class Program
                {
                    static void Main()
                    {
                        IMyCollection<string?> x = F<string>([], default!);
                        x.Report();
                        IMyCollection<int> y = F<int>([1, 2], 3);
                        y.Report();
                    }
                    static IMyCollection<T?> F<T>(ReadOnlySpan<T> items, T arg) => [with(arg), ..items];
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[null], [1, 2, 3], "));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program.F<T>", """
                {
                  // Code size       13 (0xd)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  call       "MyCollectionBuilder.MyCollection<T> MyCollectionBuilder.Create<T>(System.ReadOnlySpan<T>, T)"
                  IL_0007:  box        "MyCollectionBuilder.MyCollection<T>"
                  IL_000c:  ret
                }
                """);
        }

        [Fact]
        public void CollectionBuilder_ObsoleteBuilderMethod_01()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                    [Obsolete]
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg) => default;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c = [with()];
                        c = [with(default)];
                        c = F(1, 2);
                    }
                    static MyCollection<T> F<T>(params MyCollection<T> c) => c;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (8,14): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //         c = [with(default)];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(8, 14));
        }

        [Fact]
        public void CollectionBuilder_ObsoleteBuilderMethod_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                class MyBuilder
                {
                    [Obsolete]
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) => default;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c = [with()];
                        c = [with(default)];
                        c = F(1, 2);
                    }
                    static MyCollection<T> F<T>(params MyCollection<T> c) => c;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,13): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //         c = [];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(6, 13),
                // (7,14): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //         c = [with()];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with()").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(7, 14),
                // (8,14): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //         c = [with(default)];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(8, 14),
                // (9,13): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //         c = F(1, 2);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "F(1, 2)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(9, 13),
                // (11,33): warning CS0612: 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)' is obsolete
                //     static MyCollection<T> F<T>(params MyCollection<T> c) => c;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)").WithLocation(11, 33));
        }

        [Fact]
        public void CollectionBuilder_UnmanagedCallersOnly()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection : IEnumerable<int>
                {
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;
                }
                class MyBuilder
                {
                    [UnmanagedCallersOnly]
                    public static MyCollection Create(ReadOnlySpan<int> items, params object[] args) => default;
                }
                """;
            string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection c;
                        c = [];
                        c = [with()];
                        c = [with(0)];
                        c = F(1, 2);
                    }
                    static MyCollection F(params MyCollection c) => c;
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS8901: 'MyBuilder.Create(ReadOnlySpan<int>, params object[])' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         c = [];
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "[]").WithArguments("MyBuilder.Create(System.ReadOnlySpan<int>, params object[])").WithLocation(6, 13),
                // (7,14): error CS8901: 'MyBuilder.Create(ReadOnlySpan<int>, params object[])' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         c = [with()];
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "with()").WithArguments("MyBuilder.Create(System.ReadOnlySpan<int>, params object[])").WithLocation(7, 14),
                // (8,14): error CS8901: 'MyBuilder.Create(ReadOnlySpan<int>, params object[])' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         c = [with(0)];
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "with(0)").WithArguments("MyBuilder.Create(System.ReadOnlySpan<int>, params object[])").WithLocation(8, 14),
                // (9,13): error CS8901: 'MyBuilder.Create(ReadOnlySpan<int>, params object[])' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         c = F(1, 2);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "F(1, 2)").WithArguments("MyBuilder.Create(System.ReadOnlySpan<int>, params object[])").WithLocation(9, 13),
                // (11,27): error CS8901: 'MyBuilder.Create(ReadOnlySpan<int>, params object[])' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //     static MyCollection F(params MyCollection c) => c;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "params MyCollection c").WithArguments("MyBuilder.Create(System.ReadOnlySpan<int>, params object[])").WithLocation(11, 27),
                // (15,19): error CS8894: Cannot use 'MyCollection' as a return type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static MyCollection Create(ReadOnlySpan<int> items, params object[] args) => default;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "MyCollection").WithArguments("MyCollection", "return").WithLocation(15, 19),
                // (15,39): error CS8894: Cannot use 'ReadOnlySpan<int>' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static MyCollection Create(ReadOnlySpan<int> items, params object[] args) => default;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "ReadOnlySpan<int> items").WithArguments("System.ReadOnlySpan<int>", "parameter").WithLocation(15, 39),
                // (15,64): error CS8894: Cannot use 'object[]' as a parameter type on a method attributed with 'UnmanagedCallersOnly'.
                //     public static MyCollection Create(ReadOnlySpan<int> items, params object[] args) => default;
                Diagnostic(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, "params object[] args").WithArguments("object[]", "parameter").WithLocation(15, 64));
        }

        [Fact]
        public void CollectionBuilder_GenericConstraints_01()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection(T arg, ReadOnlySpan<T> items)
                    {
                        _items = new();
                        _items.Add(arg);
                        _items.AddRange(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(default, items);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg) where T : struct => new(arg, items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> x;
                        x = [with(), 1];
                        x.Report();
                        MyCollection<int> y;
                        y = [with(2), 3];
                        y.Report();
                        x = F((object)1);
                        x.Report();
                        y = F(3);
                        y.Report();
                    }
                    static MyCollection<T> F<T>(params MyCollection<T> c) => c;
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[null, 1], [2, 3], [null, 1], [0, 3], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> x;
                        x = [with(default)];
                        x = [with(2), 3];
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB2],
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         x = [with(default)];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(default)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(6, 14),
                // (7,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         x = [with(2), 3];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(2)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(7, 14));
        }

        [Fact]
        public void CollectionBuilder_GenericConstraints_02()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection(T arg, ReadOnlySpan<T> items)
                    {
                        _items = new();
                        _items.Add(arg);
                        _items.AddRange(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) where T : struct => new(arg, items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c.Report();
                        c = [with(), 1];
                        c.Report();
                        c = [with(2)];
                        c.Report();
                        F<int>();
                        F(3);
                        F(4, 5);
                    }
                    static void F<T>(params MyCollection<T> c) where T : struct
                    {
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[0], [0, 1], [2], [0], [0, 3], [0, 4, 5], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> c;
                        c = [];
                        c = [with(), 1];
                        c = [with(2)];
                        F<object>();
                        F((object)3);
                    }
                    static void F<T>(params MyCollection<T> c)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB2],
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         c = [];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(6, 13),
                // (7,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         c = [with(), 1];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with()").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(7, 14),
                // (8,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         c = [with(2)];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(2)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(8, 14),
                // (9,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         F<object>();
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<object>()").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(9, 9),
                // (10,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, T)'
                //         F((object)3);
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F((object)3)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, T)", "T", "object").WithLocation(10, 9));
        }

        [Fact]
        public void CollectionBuilder_GenericConstraints_03()
        {
            string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection(T[] args, ReadOnlySpan<T> items)
                    {
                        _items = new();
                        _items.AddRange(args);
                        _items.AddRange(items.ToArray());
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, params T[] args) where T : struct => new(args, items);
                }
                """;

            string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<int> c;
                        c = [];
                        c.Report();
                        c = [with(), 1];
                        c.Report();
                        c = [with(2, 3)];
                        c.Report();
                        F<int>();
                        F(4, 5);
                    }
                    static void F<T>(params MyCollection<T> c) where T : struct
                    {
                        c.Report();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB1, s_collectionExtensions],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[], [1], [2, 3], [], [4, 5], "));
            verifier.VerifyDiagnostics();

            string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection<object> c;
                        c = [];
                        c = [with(), 1];
                        c = [with(2, 3)];
                        F<object>();
                        F((object)4, 5);
                    }
                    static void F<T>(params MyCollection<T> c)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB2],
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, params T[])'
                //         c = [];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])", "T", "object").WithLocation(6, 13),
                // (7,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, params T[])'
                //         c = [with(), 1];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with()").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])", "T", "object").WithLocation(7, 14),
                // (8,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, params T[])'
                //         c = [with(2, 3)];
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(2, 3)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])", "T", "object").WithLocation(8, 14),
                // (9,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, params T[])'
                //         F<object>();
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<object>()").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])", "T", "object").WithLocation(9, 9),
                // (10,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>, params T[])'
                //         F((object)4, 5);
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F((object)4, 5)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, params T[])", "T", "object").WithLocation(10, 9));
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
