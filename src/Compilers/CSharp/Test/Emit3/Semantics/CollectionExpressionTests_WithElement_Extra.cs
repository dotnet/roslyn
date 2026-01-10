// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #DEFINE DICTIONARY_EXPRESSIONS

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.CollectionExpressions)]
public sealed class CollectionExpressionTests_WithElement_Extra : CSharpTestBase
{
    private static string? IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

    private const string s_collectionExtensions = CollectionExpressionTests.s_collectionExtensions;

    public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp14, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

    [Theory]
    [MemberData(nameof(LanguageVersions))]
    public void LanguageVersion_01(LanguageVersion languageVersion)
    {
        string source = """
                int[] a = [with()];
                """;
        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
        if (languageVersion == LanguageVersion.CSharp14)
        {
            comp.VerifyEmitDiagnostics(
                // (1,12): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // int[] a = [with()];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(1, 12),
                // (1,12): error CS9401: 'with(...)' elements are not supported for type 'int[]'
                // int[] a = [with()];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(1, 12));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (1,12): error CS9336: Collection arguments are not supported for type 'int[]'.
                // int[] a = [with()];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("int[]").WithLocation(1, 12));
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
        if (languageVersion == LanguageVersion.CSharp14)
        {
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // List<int> l = [1, with(), 3, with(capacity: 4)];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 19),
                // (2,19): error CS9400: 'with(...)' element must be the first element
                // List<int> l = [1, with(), 3, with(capacity: 4)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 19),
                // (2,30): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // List<int> l = [1, with(), 3, with(capacity: 4)];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 30),
                // (2,30): error CS9400: 'with(...)' element must be the first element
                // List<int> l = [1, with(), 3, with(capacity: 4)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 30));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS9501: Collection argument element must be the first element.
                // List<int> l = [1, with(), 3, with(capacity: 4)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 19),
                // (2,30): error CS9501: Collection argument element must be the first element.
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
        if (languageVersion == LanguageVersion.CSharp14)
        {
            comp.VerifyEmitDiagnostics(
                // (2,16): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // List<int> l = [with(x: 1), with(y: 2)];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 16),
                // (2,28): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // List<int> l = [with(x: 1), with(y: 2)];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 28),
                // (2,28): error CS9400: 'with(...)' element must be the first element
                // List<int> l = [with(x: 1), with(y: 2)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 28));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (2,28): error CS9335: Collection argument element must be the first element.
                // List<int> l = [with(x: 1), with(y: 2)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(2, 28));
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
        if (languageVersion == LanguageVersion.CSharp14)
        {
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     with(),
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(2, 5),
                // (3,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     with(arg: 0),
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(3, 5),
                // (3,5): error CS9400: 'with(...)' element must be the first element
                //     with(arg: 0),
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(3, 5),
                // (4,5): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     with(unknown: 1)];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(4, 5),
                // (4,5): error CS9400: 'with(...)' element must be the first element
                //     with(unknown: 1)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(4, 5));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (3,5): error CS9501: Collection argument element must be the first element.
                //     with(arg: 0),
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(3, 5),
                // (4,5): error CS9501: Collection argument element must be the first element.
                //     with(unknown: 1)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(4, 5));
        }
    }

    [Theory]
    [MemberData(nameof(LanguageVersions))]
    public void LanguageVersion_05(LanguageVersion languageVersion)
    {
        string source = """
                using System.Collections.Generic;
                List<string> list;
                list = [with(capacity: 1), "one"];
                list.Report();
                list = [@with(capacity: 2), "two"];
                list.Report();
                string with(int capacity) => $"with({capacity})";
                """;

        if (languageVersion == LanguageVersion.CSharp14)
        {
            CreateCompilation([source, s_collectionExtensions], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (3,9): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // list = [with(capacity: 1), "one"];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(3, 9));
        }
        else
        {
            var verifier = CompileAndVerify([source, s_collectionExtensions],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                expectedOutput: "[one], [with(2), two], ");
            verifier.VerifyDiagnostics();
        }
    }

    [Fact]
    public void EmptyArguments_Array1()
    {
        string source = """
                class Program
                {
                    static void Main()
                    {
                        NoArgs<int>().Report();
                    }
                    static T[] NoArgs<T>() => [];
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            expectedOutput: "[], ");
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
    }

    [Fact]
    public void EmptyArguments_Array2()
    {
        string source = """
                class Program
                {
                    static void Main()
                    {
                        EmptyArgs<int>().Report();
                    }
                    static T[] EmptyArgs<T>() => [with()];
                }
                """;
        var verifier = CreateCompilation(
            [source, s_collectionExtensions]);
        verifier.VerifyDiagnostics(
            // (7,35): error CS9336: Collection arguments are not supported for type 'T[]'.
            //     static T[] EmptyArgs<T>() => [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("T[]").WithLocation(7, 35));
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
            // (6,14): error CS9401: 'with(...)' elements are not supported for type 'T[]'
            //         a = [with(default), t];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments("T[]").WithLocation(6, 14),
            // (6,19): error CS8716: There is no target type for the default literal.
            //         a = [with(default), t];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 19),
            // (7,17): error CS9400: 'with(...)' element must be the first element
            //         a = [t, with(default)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(7, 17));

        // Collection arguments do not affect convertibility.
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
        Assert.Equal(2, collections.Length);
        VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: "T[]", ConversionKind.CollectionExpression);
        VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: "T[]", ConversionKind.NoConversion);
    }

    private static void VerifyTypes(SemanticModel model, ExpressionSyntax expr, string? expectedType, string expectedConvertedType, ConversionKind expectedConversionKind)
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
        var verifier = CreateCompilation(
            [source, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        verifier.VerifyDiagnostics(
            // (16,30): error CS9336: Collection arguments are not supported for type 'ReadOnlySpan<T>'.
            //         ReadOnlySpan<T> x = [with()];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments($"System.{spanType}<T>"));
    }

    [Theory]
    [InlineData("System.ReadOnlySpan<T>")]
    [InlineData("System.Span<T>")]
    public void Arguments_Span(string spanType)
    {
        string source = $$"""
                class Program
                {
                    static void F<T>(T t)
                    {
                        {{spanType}} x =
                            [with(default), t];
                        {{spanType}} y =
                            [t, with(default)];
                    }
                }
                """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (7,14): error CS9401: 'with(...)' elements are not supported for type 'Span<T>'
            //             [with(default), t];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(spanType),
            // (7,19): error CS8716: There is no target type for the default literal.
            //             [with(default), t];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 19),
            // (9,17): error CS9400: 'with(...)' element must be the first element
            //             [t, with(default)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17));
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
        verifier.VerifyIL("Program.NoArgs<T>", """
            {
              // Code size       51 (0x33)
              .maxstack  3
              .locals init (int V_0,
                            System.Span<T> V_1)
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
              IL_0016:  ldloca.s   V_1
              IL_0018:  ldc.i4.0
              IL_0019:  call       "ref T System.Span<T>.this[int].get"
              IL_001e:  ldarg.0
              IL_001f:  stobj      "T"
              IL_0024:  ldloca.s   V_1
              IL_0026:  ldc.i4.1
              IL_0027:  call       "ref T System.Span<T>.this[int].get"
              IL_002c:  ldarg.1
              IL_002d:  stobj      "T"
              IL_0032:  ret
            }
            """);
        verifier.VerifyIL("Program.EmptyArgs<T>", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
              IL_0005:  dup
              IL_0006:  ldarg.0
              IL_0007:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
              IL_000c:  dup
              IL_000d:  ldarg.1
              IL_000e:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
              IL_0013:  ret
            }
            """);
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
    [CombinatorialData]
    public void List_KnownLength_ICollection(
        [CombinatorialValues("", "with(), ", "with(3), ")] string argsPrefix)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create(1, 2, 3).Report();
                    }
                    static ICollection<T> Create<T>(params T[] items)
                    {
                        return [{{argsPrefix}} ..items];
                    }
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));
        verifier.VerifyDiagnostics();
        string expectedIL;
        switch (argsPrefix)
        {
            case "with(), ":
                expectedIL = """
                    {
                      // Code size       15 (0xf)
                      .maxstack  3
                      .locals init (T[] V_0)
                      IL_0000:  ldarg.0
                      IL_0001:  stloc.0
                      IL_0002:  newobj     "System.Collections.Generic.List<T>..ctor()"
                      IL_0007:  dup
                      IL_0008:  ldloc.0
                      IL_0009:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
                      IL_000e:  ret
                    }
                    """;
                break;

            case "with(3), ":
                expectedIL = """
                        {
                          // Code size       16 (0x10)
                          .maxstack  3
                          .locals init (T[] V_0)
                          IL_0000:  ldarg.0
                          IL_0001:  stloc.0
                          IL_0002:  ldc.i4.3
                          IL_0003:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                          IL_0008:  dup
                          IL_0009:  ldloc.0
                          IL_000a:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
                          IL_000f:  ret
                        }
                        """;
                break;
            default:
                expectedIL = """
                        {
                          // Code size       69 (0x45)
                          .maxstack  5
                          .locals init (T[] V_0,
                                        int V_1,
                                        System.Span<T> V_2,
                                        int V_3,
                                        System.ReadOnlySpan<T> V_4)
                          IL_0000:  ldarg.0
                          IL_0001:  stloc.0
                          IL_0002:  ldloc.0
                          IL_0003:  ldlen
                          IL_0004:  conv.i4
                          IL_0005:  stloc.1
                          IL_0006:  ldloc.1
                          IL_0007:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                          IL_000c:  dup
                          IL_000d:  ldloc.1
                          IL_000e:  call       "void System.Runtime.InteropServices.CollectionsMarshal.SetCount<T>(System.Collections.Generic.List<T>, int)"
                          IL_0013:  dup
                          IL_0014:  call       "System.Span<T> System.Runtime.InteropServices.CollectionsMarshal.AsSpan<T>(System.Collections.Generic.List<T>)"
                          IL_0019:  stloc.2
                          IL_001a:  ldc.i4.0
                          IL_001b:  stloc.3
                          IL_001c:  ldloca.s   V_4
                          IL_001e:  ldloc.0
                          IL_001f:  call       "System.ReadOnlySpan<T>..ctor(T[])"
                          IL_0024:  ldloca.s   V_4
                          IL_0026:  ldloca.s   V_2
                          IL_0028:  ldloc.3
                          IL_0029:  ldloca.s   V_4
                          IL_002b:  call       "int System.ReadOnlySpan<T>.Length.get"
                          IL_0030:  call       "System.Span<T> System.Span<T>.Slice(int, int)"
                          IL_0035:  call       "void System.ReadOnlySpan<T>.CopyTo(System.Span<T>)"
                          IL_003a:  ldloc.3
                          IL_003b:  ldloca.s   V_4
                          IL_003d:  call       "int System.ReadOnlySpan<T>.Length.get"
                          IL_0042:  add
                          IL_0043:  stloc.3
                          IL_0044:  ret
                        }
                        """;
                break;
        }
        verifier.VerifyIL("Program.Create<T>", expectedIL);
    }

    /// <summary>
    /// Implementation of List(int) uses a name other than capacity.
    /// </summary>
    [Fact]
    public void InterfaceTarget_ImplementationParameterName()
    {
        string sourceA = """
                namespace System
                {
                    public class Object { }
                    public abstract class ValueType { }
                    public class String { }
                    public class Type { }
                    public struct Void { }
                    public struct Boolean { }
                    public struct Int32 { }
                    public class Array { }
                    public interface IDisposable
                    {
                        void Dispose();
                    }
                }
                namespace System.Collections
                {
                    public interface IEnumerator
                    {
                        bool MoveNext();
                        object Current { get; }
                    }
                    public interface IEnumerable
                    {
                        IEnumerator GetEnumerator();
                    }
                }
                namespace System.Collections.Generic
                {
                    public interface IEnumerator<T> : IEnumerator
                    {
                        new T Current { get; }
                    }
                    public interface IEnumerable<T> : IEnumerable
                    {
                        new IEnumerator<T> GetEnumerator();
                    }
                    public interface ICollection<T> : IEnumerable<T>
                    {
                    }
                    public class List<T> : ICollection<T>
                    {
                        public List() { }
                        public List(int __c) { }
                        public void Add(T t) { }
                        public T[] ToArray() => null;
                        IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                        IEnumerator IEnumerable.GetEnumerator() => null;
                    }
                }
                """;
        string sourceB = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create(1, 2);
                    }
                    static ICollection<T> Create<T>(T x, T y)
                    {
                        return [with(capacity: 3), x, y];
                    }
                }
                """;
        var comp = CreateEmptyCompilation(
            [sourceA, sourceB],
            parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute(),
            options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (10,22): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
                //         return [with(capacity: 3), x, y];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity").WithLocation(10, 22));
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
    [CombinatorialData]
    public void InterfaceTarget_ArrayInterfaces(
        [CombinatorialValues("IEnumerable", "IReadOnlyCollection", "IReadOnlyList", "ICollection", "IList")] string typeName)
    {
        bool isMutable = typeName is "ICollection" or "IList";
        string sourceA = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create(1, 2, 3).Report();
                    }
                    static {{typeName}}<T> Create<T>(T x, T y, T z)
                    {
                        return [with(), x, y, z];
                    }
                }
                """;
        var comp = CreateCompilation(
            [sourceA, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(
            comp,
            expectedOutput: "[1, 2, 3], ");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.Create<T>", isMutable ?
            """
            {
                // Code size       27 (0x1b)
                .maxstack  3
                IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
                IL_0005:  dup
                IL_0006:  ldarg.0
                IL_0007:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                IL_000c:  dup
                IL_000d:  ldarg.1
                IL_000e:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                IL_0013:  dup
                IL_0014:  ldarg.2
                IL_0015:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                IL_001a:  ret
            }
            """ :
            """
                {
                  // Code size       36 (0x24)
                  .maxstack  4
                  IL_0000:  ldc.i4.3
                  IL_0001:  newarr     "T"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldarg.0
                  IL_0009:  stelem     "T"
                  IL_000e:  dup
                  IL_000f:  ldc.i4.1
                  IL_0010:  ldarg.1
                  IL_0011:  stelem     "T"
                  IL_0016:  dup
                  IL_0017:  ldc.i4.2
                  IL_0018:  ldarg.2
                  IL_0019:  stelem     "T"
                  IL_001e:  newobj     "<>z__ReadOnlyArray<T>..ctor(T[])"
                  IL_0023:  ret
                }
                """);

        string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create1(2, 1, 2, 3).Report();
                        Create2(2, 4, 5, 6).Report();
                    }
                    static {{typeName}}<T> Create1<T>(int c, T x, T y, T z)
                    {
                        return [with(c), x, y, z];
                    }
                    static {{typeName}}<T> Create2<T>(int c, T x, T y, T z)
                    {
                        return [with(capacity: c), x, y, z];
                    }
                }
                """;
        comp = CreateCompilation(
            [sourceB, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        if (isMutable)
        {
            verifier = CompileAndVerify(
                comp,
                expectedOutput: "[1, 2, 3], [4, 5, 6], ");
            verifier.VerifyDiagnostics();
            string expectedIL = """
                    {
                      // Code size       28 (0x1c)
                      .maxstack  3
                      IL_0000:  ldarg.0
                      IL_0001:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                      IL_0006:  dup
                      IL_0007:  ldarg.1
                      IL_0008:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                      IL_000d:  dup
                      IL_000e:  ldarg.2
                      IL_000f:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                      IL_0014:  dup
                      IL_0015:  ldarg.3
                      IL_0016:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                      IL_001b:  ret
                    }
                    """;
            verifier.VerifyIL("Program.Create1<T>", expectedIL);
            verifier.VerifyIL("Program.Create2<T>", expectedIL);
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (11,17): error CS9338: 'with(...)' element for a read-only interface must be empty if present
                //         return [with(c), x, y, z];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(11, 17),
                // (15,17): error CS9338: 'with(...)' element for a read-only interface must be empty if present
                //         return [with(capacity: c), x, y, z];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(15, 17));
        }

        string sourceC = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static {{typeName}}<T> Create1<T>(IEnumerable<T> c, T x, T y, T z)
                    {
                        return [with(c), x, y, z];
                    }
                    static {{typeName}}<T> Create2<T>(IEnumerable<T> c, T x, T y, T z)
                    {
                        return [with(collection: c), x, y, z];
                    }
                }
                """;
        comp = CreateCompilation(sourceC);
        if (isMutable)
        {
            comp.VerifyEmitDiagnostics(
                // (6,22): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.IEnumerable<T>' to 'int'
                //         return [with(c), x, y, z];
                Diagnostic(ErrorCode.ERR_BadArgType, "c").WithArguments("1", "System.Collections.Generic.IEnumerable<T>", "int").WithLocation(6, 22),
                // (10,22): error CS1739: The best overload for 'List' does not have a parameter named 'collection'
                //         return [with(collection: c), x, y, z];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "collection").WithArguments("List", "collection").WithLocation(10, 22));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (6,17): error CS9338: 'with(...)' element for a read-only interface must be empty if present
                //         return [with(c), x, y, z];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(6, 17),
                // (10,17): error CS9338: 'with(...)' element for a read-only interface must be empty if present
                //         return [with(collection: c), x, y, z];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(10, 17));
        }
    }

    [Theory]
    [CombinatorialData]
    public void CollectionArguments_CapacityAndComparer_01(
        [CombinatorialValues(
                "T[]",
                "System.ReadOnlySpan<T>",
                "System.Span<T>",
                "System.Collections.Generic.IEnumerable<T>",
                "System.Collections.Generic.IReadOnlyCollection<T>",
                "System.Collections.Generic.IReadOnlyList<T>",
                "System.Collections.Generic.ICollection<T>",
                "System.Collections.Generic.IList<T>")]
            string typeName)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Create<T>(int capacity, IEqualityComparer<T> comparer)
                    {
                        {{typeName}} c;
                        c = [];
                        c = [with()];
                        c = [with(default)];
                        c = [with(capacity)];
                        c = [with(comparer)];
                        c = [with(capacity, comparer)];
                    }
                }
                """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
        switch (typeName)
        {
            case "T[]":
            case "System.ReadOnlySpan<T>":
            case "System.Span<T>":
                comp.VerifyEmitDiagnostics(
                // (8,14): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<T>'
                //         c = [with()];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(typeName).WithLocation(8, 14),
                // (9,14): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<T>'
                //         c = [with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(typeName).WithLocation(9, 14),
                // (9,19): error CS8716: There is no target type for the default literal.
                //         c = [with(default)];
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 19),
                // (10,14): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<T>'
                //         c = [with(capacity)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(typeName).WithLocation(10, 14),
                // (11,14): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<T>'
                //         c = [with(comparer)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(typeName).WithLocation(11, 14),
                // (12,14): error CS9401: 'with(...)' elements are not supported for type 'ReadOnlySpan<T>'
                //         c = [with(capacity, comparer)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, "with").WithArguments(typeName).WithLocation(12, 14));
                break;
            case "System.Collections.Generic.IEnumerable<T>":
            case "System.Collections.Generic.IReadOnlyCollection<T>":
            case "System.Collections.Generic.IReadOnlyList<T>":
                comp.VerifyEmitDiagnostics(
                // (9,14): error CS9403: 'with(...)' element for a read-only interface must be empty if present
                //         c = [with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(9, 14),
                // (9,19): error CS8716: There is no target type for the default literal.
                //         c = [with(default)];
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(9, 19),
                // (10,14): error CS9403: 'with(...)' element for a read-only interface must be empty if present
                //         c = [with(capacity)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(10, 14),
                // (11,14): error CS9403: 'with(...)' element for a read-only interface must be empty if present
                //         c = [with(comparer)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(11, 14),
                // (12,14): error CS9403: 'with(...)' element for a read-only interface must be empty if present
                //         c = [with(capacity, comparer)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(12, 14));
                break;
            case "System.Collections.Generic.ICollection<T>":
            case "System.Collections.Generic.IList<T>":
                comp.VerifyEmitDiagnostics(
                    // (11,19): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.IEqualityComparer<T>' to 'int'
                    //         c = [with(comparer)];
                    Diagnostic(ErrorCode.ERR_BadArgType, "comparer").WithArguments("1", "System.Collections.Generic.IEqualityComparer<T>", "int").WithLocation(11, 19),
                    // (12,14): error CS1729: 'List<T>' does not contain a constructor that takes 2 arguments
                    //         c = [with(capacity, comparer)];
                    Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<T>", "2").WithLocation(12, 14));
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(typeName);
        }
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
        if (interfaceType is "ICollection" or "IList")
        {
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS9501: Collection argument element must be the first element.
                //         i = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9403: 'with(...)' element for a read-only interface must be empty if present
                //         i = [with(default), t];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeEmpty, "with").WithLocation(7, 14),
                // (7,19): error CS8716: There is no target type for the default literal.
                //         i = [with(default), t];
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 19),
                // (8,17): error CS9400: 'with(...)' element must be the first element
                //         i = [t, with(default)];
                Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17));
        }

        // Collection arguments do not affect convertibility.
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var collections = tree.GetRoot().DescendantNodes().OfType<CollectionExpressionSyntax>().ToArray();
        Assert.Equal(2, collections.Length);
        VerifyTypes(model, collections[0], expectedType: null, expectedConvertedType: $"System.Collections.Generic.{interfaceType}<T>", ConversionKind.CollectionExpression);
        VerifyTypes(model, collections[1], expectedType: null, expectedConvertedType: $"System.Collections.Generic.{interfaceType}<T>", ConversionKind.NoConversion);
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
            // (13,47): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'MyCollection<T>.MyCollection(T)'
            //     static MyCollection<T> EmptyArgs<T>() => [with()];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with()").WithArguments("arg", "MyCollection<T>.MyCollection(T)").WithLocation(13, 47));
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
            // (7,14): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
            //         c = [with(default)];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(7, 14));
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
            // (6,14): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
            //         c = [with()];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with()").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(6, 14),
            // (7,14): warning CS0612: 'MyCollection<int>.MyCollection(int)' is obsolete
            //         c = [with(default)];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyCollection<int>.MyCollection(int)").WithLocation(7, 14),
            // (9,22): warning CS0612: 'MyCollection<T>.MyCollection(T)' is obsolete
            //     static void F<T>(params MyCollection<T> c) { }
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "params MyCollection<T> c").WithArguments("MyCollection<T>.MyCollection(T)").WithLocation(9, 22));
    }

    [Fact]
    public void TypeInference_CollectionBuilder()
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
                        _items.AddRange(items.ToArray());
                        _items.AddRange(args);
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, params T[] args) => new(args, items);
                }
                """;
        string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Identity([with()]);
                        Identity([with(default, 2), default]);
                        Identity([with(default), default, 3]);
                    }
                    static MyCollection<T> Identity<T>(MyCollection<T> c) => c;
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS0411: The type arguments for method 'Program.Identity<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with()]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(MyCollection<T>)").WithLocation(5, 9),
            // (6,9): error CS0411: The type arguments for method 'Program.Identity<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with(default, 2), default]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(MyCollection<T>)").WithLocation(6, 9),
            // (7,18): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         Identity([with(default), default, 3]);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(default), default, 3]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 18),
            // (7,24): error CS8716: There is no target type for the default literal.
            //         Identity([with(default), default, 3]);
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 24),
            // (7,34): error CS8716: There is no target type for the default literal.
            //         Identity([with(default), default, 3]);
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 34));
    }

    [Fact]
    public void TypeInference_CollectionBuilder_Nullable()
    {
        string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection(ReadOnlySpan<T> items)
                    {
                    }
                    public IEnumerator<T> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(int length, ReadOnlySpan<T> items) => new(items);
                }
                """;
        string sourceB = """
                #nullable enable
                using System;
                class Program
                {
                    static void Main()
                    {
                        string? s = null;
                        Identity<int>([with((s = "").Length)]);
                        Console.WriteLine(s.Length);
                    }
                    static MyCollection<T> Identity<T>(MyCollection<T> c) => c;
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods()
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
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (15,53): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t), t];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(t)").WithArguments("Create", "1").WithLocation(15, 53));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_A()
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
                    public static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(arg, items);
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
            expectedOutput: IncludeExpectedOutput("0, [1], 2, [2], "),
            verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.ToArray()[1]);
        var root = semanticModel.SyntaxTree.GetRoot();
        var withElements = root.DescendantNodes().OfType<WithElementSyntax>().ToArray();
        Assert.Equal(2, withElements.Length);

        var method1 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[0]).Symbol;
        var method2 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[1]).Symbol;

        Assert.NotNull(method1);
        Assert.NotNull(method2);

        Assert.NotEqual(method1, method2);

        Assert.Equal("MyBuilder", method1.ContainingType.Name);

        AssertEx.Equal("MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T> items)", method1.ToTestDisplayString());
        AssertEx.Equal("MyCollection<T> MyBuilder.Create<T>(T arg, System.ReadOnlySpan<T> items)", method2.ToTestDisplayString());

        var arrowExpressions = root.DescendantNodes().OfType<ArrowExpressionClauseSyntax>().ToArray();
        var operation1 = semanticModel.GetOperation(arrowExpressions[0]);
        VerifyOperationTree(compilation, operation1, """
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> [with(), t]')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '[with(), t]')
                ReturnedValue:
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<T>, IsImplicit) (Syntax: '[with(), t]')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T> items)) (OperationKind.CollectionExpression, Type: MyCollection<T>) (Syntax: '[with(), t]')
                        ConstructArguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with()')
                              ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<T>, IsImplicit) (Syntax: 'with()')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Elements(1):
                            IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
            """);
        var operation2 = semanticModel.GetOperation(arrowExpressions[1]);
        VerifyOperationTree(compilation, operation2, """
            IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> [with(t), t]')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '[with(t), t]')
                ReturnedValue:
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyCollection<T>, IsImplicit) (Syntax: '[with(t), t]')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ICollectionExpressionOperation (1 elements, ConstructMethod: MyCollection<T> MyBuilder.Create<T>(T arg, System.ReadOnlySpan<T> items)) (OperationKind.CollectionExpression, Type: MyCollection<T>) (Syntax: '[with(t), t]')
                        ConstructArguments(2):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 't')
                              IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: items) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'with(t)')
                              ICollectionExpressionElementsPlaceholderOperation (OperationKind.CollectionExpressionElementsPlaceholder, Type: System.ReadOnlySpan<T>, IsImplicit) (Syntax: 'with(t)')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Elements(1):
                            IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
            """);
    }

    [Fact]
    public void IList_With_SemanticModel()
    {
        var source = """
            using System.Collections.Generic;
            class Program
            {
                static void Main()
                {
                    IList<int> x = [with(), 1, 2, 3];
                    IList<int> y = [with(capacity: 6), 1, 2, 3];
                }
            }
            """;

        var verifier = CompileAndVerify(source);
        verifier.VerifyDiagnostics();

        var compilation = (CSharpCompilation)verifier.Compilation;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot();
        var withElements = root.DescendantNodes().OfType<WithElementSyntax>().ToArray();
        Assert.Equal(2, withElements.Length);

        var method1 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[0]).Symbol;
        var method2 = (IMethodSymbol?)semanticModel.GetSymbolInfo(withElements[1]).Symbol;

        AssertEx.Equal("System.Collections.Generic.List<System.Int32>..ctor()", method1.ToTestDisplayString());
        AssertEx.Equal("System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)", method2.ToTestDisplayString());

        var operation = semanticModel.GetOperation(root.DescendantNodes().OfType<BlockSyntax>().Single());
        VerifyOperationTree(compilation, operation, """
            IBlockOperation (2 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            Locals: Local_1: System.Collections.Generic.IList<System.Int32> x
              Local_2: System.Collections.Generic.IList<System.Int32> y
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'IList<int>  ... , 1, 2, 3];')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'IList<int>  ... ), 1, 2, 3]')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<System.Int32> x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = [with(), 1, 2, 3]')
                      Initializer:
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= [with(), 1, 2, 3]')
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
                                Elements(3):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                Initializer:
                  null
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'IList<int>  ... , 1, 2, 3];')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'IList<int>  ... ), 1, 2, 3]')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IList<System.Int32> y) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y = [with(c ... ), 1, 2, 3]')
                      Initializer:
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= [with(cap ... ), 1, 2, 3]')
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                ConstructArguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 6')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Elements(3):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                Initializer:
                  null
            """);

        var (graph, symbol) = ControlFlowGraphVerifier.GetControlFlowGraph(root.DescendantNodes().OfType<BlockSyntax>().Single(), semanticModel);
        ControlFlowGraphVerifier.VerifyGraph(compilation, """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1}
            .locals {R1}
            {
                Locals: [System.Collections.Generic.IList<System.Int32> x] [System.Collections.Generic.IList<System.Int32> y]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (2)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'x = [with(), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'x = [with(), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: '[with(), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor()) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: '[with(), 1, 2, 3]')
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'y = [with(c ... ), 1, 2, 3]')
                          Left:
                            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: 'y = [with(c ... ), 1, 2, 3]')
                          Right:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IList<System.Int32>, IsImplicit) (Syntax: '[with(capac ... ), 1, 2, 3]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (CollectionExpression)
                              Operand:
                                ICollectionExpressionOperation (3 elements, ConstructMethod: System.Collections.Generic.List<System.Int32>..ctor(System.Int32 capacity)) (OperationKind.CollectionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: '[with(capac ... ), 1, 2, 3]')
                                  ConstructArguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: capacity) (OperationKind.Argument, Type: null) (Syntax: 'capacity: 6')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Elements(3):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    Next (Regular) Block[B2]
                        Leaving: {R1}
            }
            Block[B2] - Exit
                Predecessors: [B1]
                Statements (0)
            """, graph, symbol);
    }

    private static string CreateCustomListDefinition(string constructors)
    {
        return $$"""
            namespace System.Collections.Generic
            {
                public class List<T> : IList<T>
                {
                    {{constructors}}
            
                    public T this[int index] { get => throw null; set => throw null; }
                    public int Count => throw null;
                    public bool IsReadOnly => throw null;
                    public void Add(T item) { }
                    public void Clear() => throw null;
                    public bool Contains(T item) => throw null;
                    public void CopyTo(T[] array, int arrayIndex) => throw null;
                    public IEnumerator<T> GetEnumerator() => throw null;
                    public int IndexOf(T item) => throw null;
                    public void Insert(int index, T item) => throw null;
                    public bool Remove(T item) => throw null;
                    public void RemoveAt(int index) => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                    public T[] ToArray() => throw null;
                }
            }
            """;
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_DifferentCapacityName(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(cap: 6), 1, 2, 3];
                }
            }
            """;

        var compilation = CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { System.Console.Write("empty "); } public List(int cap) { System.Console.Write(cap); }""")],
            options: TestOptions.ReleaseExe);
        CompileAndVerify(compilation, expectedOutput: "empty 6").VerifyDiagnostics();
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_DifferentCapacityName2(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(capacity: 6), 1, 2, 3];
                }
            }
            """;

        CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { } public List(int cap) { }""")]).VerifyDiagnostics(
            // (8,45): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
            //         ICollection<System.Int32> y = [with(capacity: 6), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity"));
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_DifferentCapacityType(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(capacity: 6), 1, 2, 3];
                    {{typeName}} z = [with(6), 1, 2, 3];
                }
            }
            """;

        CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { } public List(long capacity) { }""")]).VerifyDiagnostics(
                // (7,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> x = [with(), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> y = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(capacity: 6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,45): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
                //         ICollection<System.Int32> y = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity"),
                // (9,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (9,40): error CS1729: 'List<int>' does not contain a constructor that takes 1 arguments
                //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "1"));
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_ParamsCapacity(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(6), 1, 2, 3];
                }
            }
            """;

        CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { } public List(params int[] capacity) { }""")]).VerifyDiagnostics(
            // (7,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
            //         ICollection<System.Int32> x = [with(), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
            // (8,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
            //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
            // (8,40): error CS1729: 'List<int>' does not contain a constructor that takes 1 arguments
            //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
            Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "1"));
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_OptionalIntCapacity(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(6), 1, 2, 3];
                    {{typeName}} z = [with(capacity: 5), 1, 2, 3];
                }
            }
            """;

        var compilation = CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { System.Console.Write("empty "); } public List(int capacity = 0) { System.Console.Write(capacity + " "); }""")],
            options: TestOptions.ReleaseExe);
        CompileAndVerify(compilation, expectedOutput: "empty 6 5").VerifyDiagnostics();
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_OptionalNonIntCapacity(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} x = [with(), 1, 2, 3];
                    {{typeName}} y = [with(6), 1, 2, 3];
                    {{typeName}} z = [with(capacity: 6), 1, 2, 3];
                }
            }
            """;

        CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { } public List(long capacity = 0) { }""")]).VerifyDiagnostics(
                // (7,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> x = [with(), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,40): error CS1729: 'List<int>' does not contain a constructor that takes 1 arguments
                //         ICollection<System.Int32> y = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "1"),
                // (9,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> z = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(capacity: 6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (9,45): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
                //         ICollection<System.Int32> z = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity"));
    }

    [Theory]
    [InlineData("IList<System.Int32>")]
    [InlineData("ICollection<System.Int32>")]
    public void IList_With_OptionalParamsAfterCapacity(string typeName)
    {
        var source = $$"""
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    {{typeName}} a = [with(), 1, 2, 3];
                    {{typeName}} b = [with(6), 1, 2, 3];
                    {{typeName}} c = [with(capacity: 6), 1, 2, 3];
                    {{typeName}} d = [with(6, 0), 1, 2, 3];
                    {{typeName}} e = [with(capacity: 6, other: 0), 1, 2, 3];
                    {{typeName}} f = [with(other: 6, capacity: 0), 1, 2, 3];
                }
            }
            """;

        CreateCompilation([
            source,
            CreateCustomListDefinition("""public List() { } public List(int capacity, int other = 0) { }""")]).VerifyDiagnostics(
                // (7,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> a = [with(), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> b = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (8,40): error CS1729: 'List<int>' does not contain a constructor that takes 1 arguments
                //         ICollection<System.Int32> b = [with(6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "1"),
                // (9,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> c = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(capacity: 6), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (9,45): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
                //         ICollection<System.Int32> c = [with(capacity: 6), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity"),
                // (10,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> d = [with(6, 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(6, 0), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (10,40): error CS1729: 'List<int>' does not contain a constructor that takes 2 arguments
                //         ICollection<System.Int32> d = [with(6, 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "with").WithArguments("System.Collections.Generic.List<int>", "2"),
                // (11,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> e = [with(capacity: 6, other: 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(capacity: 6, other: 0), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (11,45): error CS1739: The best overload for 'List' does not have a parameter named 'capacity'
                //         ICollection<System.Int32> e = [with(capacity: 6, other: 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("List", "capacity"),
                // (12,39): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                //         ICollection<System.Int32> f = [with(other: 6, capacity: 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(other: 6, capacity: 0), 1, 2, 3]").WithArguments("System.Collections.Generic.List`1", ".ctor"),
                // (12,45): error CS1739: The best overload for 'List' does not have a parameter named 'other'
                //         ICollection<System.Int32> f = [with(other: 6, capacity: 0), 1, 2, 3];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "other").WithArguments("List", "other"));
    }

    [Fact]
    public void CollectionBuilder_PrivateMethod1()
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
                    private static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(arg, items);
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

        CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (15,53): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
                //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t), t];
                Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(t)").WithArguments("Create", "1"));
    }

    [Fact]
    public void CollectionBuilder_PrivateMethod2()
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
                static partial class MyBuilder
                {
                    private static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(default, items);
                    private static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(arg, items);
                }
                """;
        string sourceB = """
                using System;
                static partial class MyBuilder
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

        CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilder_InternalMethod()
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
                    internal static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(default, items);
                    internal static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(arg, items);
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

        CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilder_ProtectedMethod()
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
                partial class MyBuilder
                {
                    protected static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(default, items);
                    protected static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(arg, items);
                }
                """;
        string sourceB = """
                using System;
                partial class MyBuilder
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

        CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80).VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilder_NoBuilderMethodsRefSpanElementType()
    {
        string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection : IEnumerable<ReadOnlySpan<int>>
                {
                    public IEnumerator<ReadOnlySpan<int>> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                class MyBuilder
                {
                }
                """;
        string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        MyCollection c = [];
                    }
                }
                """;

        CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (5,26): error CS9404: Element type of this collection may not be a ref struct or a type parameter allowing ref structs
                //         MyCollection c = [];
                Diagnostic(ErrorCode.ERR_CollectionRefLikeElementType, "[]").WithLocation(5, 26),
                // (5,26): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<System.ReadOnlySpan<int>>' and return type 'MyCollection'.
                //         MyCollection c = [];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "System.ReadOnlySpan<int>", "MyCollection"));
    }

    [Fact]
    public void CollectionBuilder_NoParameterlessBuilderMethod()
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
            // (5,52): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(t)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(5, 52),
            // (6,38): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 38));
    }

    [Fact]
    public void CollectionBuilder_NoParameterlessBuilderMethod_A()
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
                    public static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => default;
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
            // (4,47): error CS7036: There is no argument given that corresponds to the required parameter 'arg' of 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //     static MyCollection<T> EmptyArgs<T>() => [with()];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with()").WithArguments("arg", "MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(4, 47),
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
        var comp = CreateCompilation(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (8,27): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         Console.WriteLine(Params(3, 4).Arg);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Params(3, 4)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 27),
            // (10,46): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> EmptyArgs<T>() => [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 46),
            // (11,52): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> NonEmptyArgs<T>(T t) => [with(t)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(t)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 52),
            // (12,38): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 38));
    }

    [Fact]
    public void CollectionBuilder_OptionalParameter_A()
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
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) => new(arg, items);
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
        var comp = CreateCompilation(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (12,38): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 38));
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
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         Params(6).Args.Report();
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "Params(6)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 9),
            // (11,46): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> EmptyArgs<T>() => [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 46),
            // (12,46): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> OneArg<T>(T t) => [with(t)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(t)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 46),
            // (13,52): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> TwoArgs<T>(T x, T y) => [with(x, y)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(x, y)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(13, 52),
            // (14,57): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> MultipleArgs<T>(T[] args) => [with(args)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(args)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(14, 57),
            // (15,38): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(15, 38));
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
            // (6,13): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(1)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(1)").WithArguments("Create", "1").WithLocation(6, 14),
            // (7,13): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(2), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(2)").WithArguments("Create", "1").WithLocation(7, 14));
    }

    [Fact]
    public void CollectionBuilder_ImplicitParameter_Params_01()
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
            // (6,13): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(1)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(1)").WithArguments("Create", "1").WithLocation(6, 14),
            // (7,13): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(2), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(2)").WithArguments("Create", "1").WithLocation(7, 14));
    }

    [Fact]
    public void CollectionBuilder_ImplicitParameter_Params_02()
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
        string sourceB = """
                using System;
                class MyItem
                {
                    public static implicit operator MyItem(ReadOnlySpan<MyItem> items) => new();
                }
                class Program
                {
                    static void Main()
                    {
                        MyItem x = new();
                        MyItem y = new();
                        MyCollection<MyItem> c;
                        c = [];
                        c = [x, y];
                        c = [with(x)];
                        c = [with(x), y];
                        c = [with(), x, y];
                    }
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);

        comp.VerifyEmitDiagnostics(
            // (15,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(15, 14),
            // (16,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(x), y];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(16, 14));
    }

    // C#7.3 feature ImprovedOverloadCandidates drops candidates with constraint violations
    // (see OverloadResolution.RemoveConstraintViolations()) which allows constructing
    // MyCollection<T> and MyCollection<U> with different factory methods.
    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_ClassAndStruct()
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
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB1], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (3,58): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(3, 58));

        string sourceB2 = """
                class Program
                {
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                }
                """;
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (3,58): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "NoConstraintsParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(3, 58),
            // (4,51): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(4, 51));

        string sourceB3 = """
                class Program
                {
                    static void Main()
                    {
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB3, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (8,78): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(8, 78));

        string sourceB4 = """
                class Program
                {
                    static void Main()
                    {
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB4, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (8,78): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "StructConstraintParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(8, 78),
            // (10,54): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(10, 54));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_ClassAndStruct_A()
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
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items) where T : struct => new(arg, items);
                }
                """;

        string sourceB1 = """
                class Program
                {
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB1], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (3,58): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(3, 58),
            // (25,83): error CS1737: Optional parameters must appear after all required parameters
            //     public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items) where T : struct => new(arg, items);
            Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(25, 83));

        string sourceB2 = """
                class Program
                {
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                }
                """;
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (3,58): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "NoConstraintsParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(3, 58),
            // (4,51): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(4, 51),
            // (25,83): error CS1737: Optional parameters must appear after all required parameters
            //     public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items) where T : struct => new(arg, items);
            Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(25, 83));

        string sourceB3 = """
                class Program
                {
                    static void Main()
                    {
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB3, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (8,78): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(8, 78),
            // (25,83): error CS1737: Optional parameters must appear after all required parameters
            //     public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items) where T : struct => new(arg, items);
            Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(25, 83));

        string sourceB4 = """
                class Program
                {
                    static void Main()
                    {
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB4, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (8,78): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "StructConstraintParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(8, 78),
            // (10,54): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
            Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(10, 54),
            // (25,83): error CS1737: Optional parameters must appear after all required parameters
            //     public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items) where T : struct => new(arg, items);
            Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(25, 83));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_NoneAndClass()
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(items);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) where T : class => new(arg, items);
                }
                """;

        string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        var verifier = CompileAndVerify(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2], [3, 4], [5, 6], "));
        verifier.VerifyDiagnostics();
        string expectedIL = """
                {
                  // Code size       50 (0x32)
                  .maxstack  2
                  .locals init (<>y__InlineArray2<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray2<T>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_0010:  ldarg.0
                  IL_0011:  stobj      "T"
                  IL_0016:  ldloca.s   V_0
                  IL_0018:  ldc.i4.1
                  IL_0019:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_001e:  ldarg.1
                  IL_001f:  stobj      "T"
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  ldc.i4.2
                  IL_0027:  call       "System.ReadOnlySpan<T> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<T>, T>(in <>y__InlineArray2<T>, int)"
                  IL_002c:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>)"
                  IL_0031:  ret
                }
                """;
        verifier.VerifyIL("Program.NoConstraints<T>", expectedIL);
        verifier.VerifyIL("Program.StructConstraint<T>", expectedIL);
        verifier.VerifyIL("Program.ClassConstraint<T>", expectedIL);

        string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        verifier = CompileAndVerify(
            [sourceA, sourceB2, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2], [3, 4], [5, 6], "));
        verifier.VerifyDiagnostics();
        expectedIL = """
                {
                  // Code size       55 (0x37)
                  .maxstack  2
                  .locals init (<>y__InlineArray2<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray2<T>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_0010:  ldarg.0
                  IL_0011:  stobj      "T"
                  IL_0016:  ldloca.s   V_0
                  IL_0018:  ldc.i4.1
                  IL_0019:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_001e:  ldarg.1
                  IL_001f:  stobj      "T"
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  ldc.i4.2
                  IL_0027:  call       "System.ReadOnlySpan<T> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<T>, T>(in <>y__InlineArray2<T>, int)"
                  IL_002c:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>)"
                  IL_0031:  call       "MyCollection<T> Program.NoConstraintsParams<T>(params MyCollection<T>)"
                  IL_0036:  ret
                }
                """;
        verifier.VerifyIL("Program.NoConstraints<T>", expectedIL);
        verifier.VerifyIL("Program.StructConstraint<T>", expectedIL.Replace("NoConstraintsParams", "StructConstraintParams"));
        verifier.VerifyIL("Program.ClassConstraint<T>", expectedIL.Replace("NoConstraintsParams", "ClassConstraintParams"));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_NoneAndClass_A()
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => new(items);
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) where T : class => new(arg, items);
                }
                """;

        string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        var verifier = CompileAndVerify(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2], [3, 4], [5, 6], "));
        verifier.VerifyDiagnostics();
        string expectedIL = """
                {
                  // Code size       50 (0x32)
                  .maxstack  2
                  .locals init (<>y__InlineArray2<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray2<T>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_0010:  ldarg.0
                  IL_0011:  stobj      "T"
                  IL_0016:  ldloca.s   V_0
                  IL_0018:  ldc.i4.1
                  IL_0019:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_001e:  ldarg.1
                  IL_001f:  stobj      "T"
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  ldc.i4.2
                  IL_0027:  call       "System.ReadOnlySpan<T> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<T>, T>(in <>y__InlineArray2<T>, int)"
                  IL_002c:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>)"
                  IL_0031:  ret
                }
                """;
        verifier.VerifyIL("Program.NoConstraints<T>", expectedIL);
        verifier.VerifyIL("Program.StructConstraint<T>", expectedIL);
        verifier.VerifyIL("Program.ClassConstraint<T>", expectedIL);

        string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        verifier = CompileAndVerify(
            [sourceA, sourceB2, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2], [3, 4], [5, 6], "));
        verifier.VerifyDiagnostics();
        expectedIL = """
                {
                  // Code size       55 (0x37)
                  .maxstack  2
                  .locals init (<>y__InlineArray2<T> V_0)
                  IL_0000:  ldloca.s   V_0
                  IL_0002:  initobj    "<>y__InlineArray2<T>"
                  IL_0008:  ldloca.s   V_0
                  IL_000a:  ldc.i4.0
                  IL_000b:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_0010:  ldarg.0
                  IL_0011:  stobj      "T"
                  IL_0016:  ldloca.s   V_0
                  IL_0018:  ldc.i4.1
                  IL_0019:  call       "ref T <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<T>, T>(ref <>y__InlineArray2<T>, int)"
                  IL_001e:  ldarg.1
                  IL_001f:  stobj      "T"
                  IL_0024:  ldloca.s   V_0
                  IL_0026:  ldc.i4.2
                  IL_0027:  call       "System.ReadOnlySpan<T> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<T>, T>(in <>y__InlineArray2<T>, int)"
                  IL_002c:  call       "MyCollection<T> MyBuilder.Create<T>(System.ReadOnlySpan<T>)"
                  IL_0031:  call       "MyCollection<T> Program.NoConstraintsParams<T>(params MyCollection<T>)"
                  IL_0036:  ret
                }
                """;
        verifier.VerifyIL("Program.NoConstraints<T>", expectedIL);
        verifier.VerifyIL("Program.StructConstraint<T>", expectedIL.Replace("NoConstraintsParams", "StructConstraintParams"));
        verifier.VerifyIL("Program.ClassConstraint<T>", expectedIL.Replace("NoConstraintsParams", "ClassConstraintParams"));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_StructAndNone()
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) where T : struct => new(items);
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, T arg = default) => new(arg, items);
                }
                """;

        string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,58): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(9, 58),
            // (11,76): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(11, 76));

        string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB2, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,58): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "NoConstraintsParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(9, 58),
            // (11,76): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "ClassConstraintParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(11, 76),
            // (12,51): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(12, 51),
            // (14,53): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(14, 53));
    }

    [Fact]
    public void CollectionBuilder_MultipleBuilderMethods_GenericConstraints_StructAndNone_A()
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) where T : struct => new(items);
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) => new(arg, items);
                }
                """;

        string sourceB1 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => [x, y];
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,58): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => [x, y];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(9, 58),
            // (11,76): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => [x, y];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[x, y]").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(11, 76));

        string sourceB2 = """
                class Program
                {
                    static void Main()
                    {
                        NoConstraints(1, 2).Report();
                        StructConstraint(3, 4).Report();
                        ClassConstraint((object)5, 6).Report();
                    }
                    static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
                    static MyCollection<T> StructConstraint<T>(T x, T y) where T : struct => StructConstraintParams(x, y);
                    static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
                    static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
                    static MyCollection<T> StructConstraintParams<T>(params MyCollection<T> c) where T : struct => c;
                    static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
                }
                """;
        comp = CreateCompilation(
            [sourceA, sourceB2, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,58): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraints<T>(T x, T y) => NoConstraintsParams(x, y);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "NoConstraintsParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(9, 58),
            // (11,76): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraint<T>(T x, T y) where T : class => ClassConstraintParams(x, y);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "ClassConstraintParams(x, y)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(11, 76),
            // (12,51): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> NoConstraintsParams<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(12, 51),
            // (14,53): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(ReadOnlySpan<T>)'
            //     static MyCollection<T> ClassConstraintParams<T>(params MyCollection<T> c) where T : class => c;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "params MyCollection<T> c").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>)", "T", "T").WithLocation(14, 53));
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
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (2,11): error CS1739: The best overload for 'Create' does not have a parameter named 'x'
            // c = [with(x: 1), 2, 3];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("Create", "x").WithLocation(2, 11),
            // (4,11): error CS1739: The best overload for 'Create' does not have a parameter named 'y'
            // c = [with(y: 4), 5];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "y").WithArguments("Create", "y").WithLocation(4, 11),
            // (6,11): error CS1739: The best overload for 'Create' does not have a parameter named 'y'
            // c = [with(y: 6, x: 7), 8];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "y").WithArguments("Create", "y").WithLocation(6, 11));
    }

    [Fact]
    public void CollectionBuilder_NamedParameter_A()
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
                    public static MyCollection<T> Create<T>(T x = default, T y = default, ReadOnlySpan<T> items = default) => new(items, x, y);
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
            expectedOutput: IncludeExpectedOutput("[1, 0, 2, 3], [0, 4, 5], [7, 6, 8], "),
            verify: Verification.FailsPEVerify);
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref x)").WithArguments("Create", "1").WithLocation(5, 6),
            // (8,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref r)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref r)").WithArguments("Create", "1").WithLocation(8, 6));

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
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(0)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(0)").WithArguments("Create", "1").WithLocation(5, 6),
            // (6,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(6, 6),
            // (7,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in x)").WithArguments("Create", "1").WithLocation(7, 6),
            // (8,15): error CS1510: A ref or out value must be an assignable variable
            // c = [with(ref ro)];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(8, 15),
            // (9,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(out x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(out x)").WithArguments("Create", "1").WithLocation(9, 6));
    }

    [Fact]
    public void CollectionBuilder_RefParameter_A()
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
                    public static MyCollection<T> Create<T>(ref T x, ReadOnlySpan<T> items) => new(items, x);
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
            expectedOutput: IncludeExpectedOutput("[1], [2], "),
            verify: Verification.FailsPEVerify);
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
            // (5,11): error CS1620: Argument 1 must be passed with the 'ref' keyword
            // c = [with(0)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "0").WithArguments("1", "ref").WithLocation(5, 11),
            // (6,11): error CS1620: Argument 1 must be passed with the 'ref' keyword
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(6, 11),
            // (7,14): error CS1620: Argument 1 must be passed with the 'ref' keyword
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(7, 14),
            // (8,15): error CS1510: A ref or out value must be an assignable variable
            // c = [with(ref ro)];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(8, 15),
            // (9,15): error CS1620: Argument 1 must be passed with the 'ref' keyword
            // c = [with(out x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "ref").WithLocation(9, 15));
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,5): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(0)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(0)").WithArguments("Create", "1").WithLocation(6, 6),
            // (8,5): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(8, 6),
            // (11,5): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref x)").WithArguments("Create", "1").WithLocation(11, 6),
            // (14,5): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref r)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref r)").WithArguments("Create", "1").WithLocation(14, 6),
            // (17,5): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in ro)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in ro)").WithArguments("Create", "1").WithLocation(17, 6));

        string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref readonly int ro = ref x;
                c = [with(in x)];
                c = [with(ref ro)];
                c = [with(out x)];
                """;
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in x)").WithArguments("Create", "1").WithLocation(5, 6),
            // (6,15): error CS1510: A ref or out value must be an assignable variable
            // c = [with(ref ro)];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(6, 15),
            // (7,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(out x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(out x)").WithArguments("Create", "1").WithLocation(7, 6));
    }

    [Fact]
    public void CollectionBuilder_RefReadonlyParameter_A()
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
                    public static MyCollection<T> Create<T>(ref readonly T x, ReadOnlySpan<T> items) => new(items, x);
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
        CompileAndVerify(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput("[0], [1], [2], [3], [4], "), verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (6,11): warning CS9193: Argument 1 should be a variable because it is passed to a 'ref readonly' parameter
            // c = [with(0)];
            Diagnostic(ErrorCode.WRN_RefReadonlyNotVariable, "0").WithArguments("1").WithLocation(6, 11),
            // (8,11): warning CS9192: Argument 1 should be passed with 'ref' or 'in' keyword
            // c = [with(x)];
            Diagnostic(ErrorCode.WRN_ArgExpectedRefOrIn, "x").WithArguments("1").WithLocation(8, 11));

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
            // (7,15): error CS1615: Argument 1 may not be passed with the 'out' keyword
            // c = [with(out x)];
            Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "out").WithLocation(7, 15));
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
        var comp = CreateCompilation([sourceA, sourceB1, s_collectionExtensions], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(0)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(0)").WithArguments("Create", "1").WithLocation(6, 6),
            // (8,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(8, 6),
            // (11,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref x)").WithArguments("Create", "1").WithLocation(11, 6),
            // (14,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in x)").WithArguments("Create", "1").WithLocation(14, 6),
            // (17,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in r)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in r)").WithArguments("Create", "1").WithLocation(17, 6),
            // (20,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in ro)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in ro)").WithArguments("Create", "1").WithLocation(20, 6));

        string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                ref readonly int ro = ref x;
                c = [with(ref ro)];
                """;
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,15): error CS1510: A ref or out value must be an assignable variable
            // c = [with(ref ro)];
            Diagnostic(ErrorCode.ERR_RefLvalueExpected, "ro").WithLocation(5, 15));
    }

    [Fact]
    public void CollectionBuilder_InParameter_A()
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
                    public static MyCollection<T> Create<T>(in T x, ReadOnlySpan<T> items) => new(items, x);
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
        CompileAndVerify(
            [sourceA, sourceB1, s_collectionExtensions], targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput("[0], [1], [2], [3], [4], [5], "), verify: Verification.FailsPEVerify).VerifyDiagnostics(
            // (11,15): warning CS9191: The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
            // c = [with(ref x)];
            Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("1").WithLocation(11, 15));

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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(out x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(out x)").WithArguments("Create", "1").WithLocation(5, 6),
            // (8,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(out r), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(out r)").WithArguments("Create", "1").WithLocation(8, 6));

        string sourceB2 = """
                #pragma warning disable 219 // variable assigned but never used
                MyCollection<int> c;
                int x = 1;
                c = [with(1)];
                c = [with(x)];
                c = [with(ref x)];
                c = [with(in x)];
                """;
        comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (4,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(1)").WithArguments("Create", "1").WithLocation(4, 6),
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x)").WithArguments("Create", "1").WithLocation(5, 6),
            // (6,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(ref x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(ref x)").WithArguments("Create", "1").WithLocation(6, 6),
            // (7,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in x)").WithArguments("Create", "1").WithLocation(7, 6));
    }

    [Fact]
    public void CollectionBuilder_OutParameter_A()
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
                    public static MyCollection<T> Create<T>(out T x, ReadOnlySpan<T> items) { x = default; return new(items, x); }
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
            expectedOutput: IncludeExpectedOutput("[0], [3, 0], "),
            verify: Verification.FailsPEVerify);
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
            // (4,11): error CS1620: Argument 1 must be passed with the 'out' keyword
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "1").WithArguments("1", "out").WithLocation(4, 11),
            // (5,11): error CS1620: Argument 1 must be passed with the 'out' keyword
            // c = [with(x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "out").WithLocation(5, 11),
            // (6,15): error CS1620: Argument 1 must be passed with the 'out' keyword
            // c = [with(ref x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "out").WithLocation(6, 15),
            // (7,14): error CS1620: Argument 1 must be passed with the 'out' keyword
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadArgRef, "x").WithArguments("1", "out").WithLocation(7, 14));
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
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (5,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(in x)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(in x)").WithArguments("Create", "1").WithLocation(5, 6),
            // (7,6): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            // c = [with(1), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(1)").WithArguments("Create", "1").WithLocation(7, 6),
            // (9,6): error CS9405: No overload for method 'Create' takes 2 'with(...)' element arguments
            // c = [with(x, ref y)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(x, ref y)").WithArguments("Create", "2").WithLocation(9, 6),
            // (11,6): error CS9405: No overload for method 'Create' takes 2 'with(...)' element arguments
            // c = [with(out x, y), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(out x, y)").WithArguments("Create", "2").WithLocation(11, 6));
    }

    [Fact]
    public void CollectionBuilder_RefParameter_Overloads_A()
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
                    public static MyCollection<T> Create<T>(in T x, ReadOnlySpan<T> items) => new(items, x, default);
                    public static MyCollection<T> Create<T>(T x, ref T y, ReadOnlySpan<T> items) => new(items, x, y);
                    public static MyCollection<T> Create<T>(out T x, T y, ReadOnlySpan<T> items) { x = default; return new(items, x, y); }
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
            expectedOutput: IncludeExpectedOutput("[1, 0], [1, 0, 3], [1, 2], [0, 2, 3], "),
            verify: Verification.FailsPEVerify);
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
            // (2,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(items: default)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(items: default)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(2, 5),
            // (2,18): error CS8716: There is no target type for the default literal.
            // c = [with(items: default)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(2, 18),
            // (3,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(items: default, 1)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(items: default, 1)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(3, 5),
            // (3,18): error CS8716: There is no target type for the default literal.
            // c = [with(items: default, 1)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(3, 18),
            // (4,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(items: default, arg: 2)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(items: default, arg: 2)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(4, 5),
            // (4,18): error CS8716: There is no target type for the default literal.
            // c = [with(items: default, arg: 2)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(4, 18),
            // (5,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(3, items: default)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(3, items: default)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(5, 5),
            // (5,21): error CS8716: There is no target type for the default literal.
            // c = [with(3, items: default)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(5, 21),
            // (6,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(arg: 4, items: default)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(arg: 4, items: default)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 5),
            // (6,26): error CS8716: There is no target type for the default literal.
            // c = [with(arg: 4, items: default)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 26),
            // (7,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(default, 5)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(default, 5)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 5),
            // (7,11): error CS8716: There is no target type for the default literal.
            // c = [with(default, 5)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(7, 11),
            // (8,5): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            // c = [with(default, arg: 6)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(default, arg: 6)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 5),
            // (8,11): error CS8716: There is no target type for the default literal.
            // c = [with(default, arg: 6)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 11));
    }

    [Fact]
    public void CollectionBuilder_ReferenceImplicitParameter_A()
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
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) => new(items, arg);
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
            // (2,11): error CS1739: The best overload for 'Create' does not have a parameter named 'items'
            // c = [with(items: default)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "items").WithArguments("Create", "items").WithLocation(2, 11),
            // (3,11): error CS1739: The best overload for 'Create' does not have a parameter named 'items'
            // c = [with(items: default, 1)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "items").WithArguments("Create", "items").WithLocation(3, 11),
            // (4,11): error CS1739: The best overload for 'Create' does not have a parameter named 'items'
            // c = [with(items: default, arg: 2)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "items").WithArguments("Create", "items").WithLocation(4, 11),
            // (5,14): error CS1739: The best overload for 'Create' does not have a parameter named 'items'
            // c = [with(3, items: default)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "items").WithArguments("Create", "items").WithLocation(5, 14),
            // (6,19): error CS1739: The best overload for 'Create' does not have a parameter named 'items'
            // c = [with(arg: 4, items: default)];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "items").WithArguments("Create", "items").WithLocation(6, 19),
            // (7,6): error CS9405: No overload for method 'Create' takes 2 'with(...)' element arguments
            // c = [with(default, 5)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(default, 5)").WithArguments("Create", "2").WithLocation(7, 6),
            // (8,20): error CS1744: Named argument 'arg' specifies a parameter for which a positional argument has already been given
            // c = [with(default, arg: 6)];
            Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "arg").WithArguments("arg").WithLocation(8, 20));
    }

    [Fact]
    public void CollectionBuilder_ReadOnlySpanConstraint()
    {
        string sourceA = """
                namespace System
                {
                    public ref struct ReadOnlySpan<T>
                        where T : struct
                    {
                    }
                }
                """;
        string sourceB = """
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, int arg) where T : struct => default;
                }
                """;
        string sourceC = """
                class Program
                {
                    static MyCollection<T> NoArgs<T>() => [];
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> WithArg<T>(int arg) => [with(arg)];
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB, sourceC, CollectionBuilderAttributeDefinition]);
        comp.VerifyEmitDiagnostics(
            // (5,52): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //     static MyCollection<T> WithArg<T>(int arg) => [with(arg)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(arg)").WithArguments("Create", "1").WithLocation(5, 52),
            // (13,61): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'ReadOnlySpan<T>'
            //     public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "items").WithArguments("System.ReadOnlySpan<T>", "T", "T").WithLocation(13, 61));
    }

    [Fact]
    public void CollectionBuilder_ReadOnlySpanConstraint_A()
    {
        string sourceA = """
                namespace System
                {
                    public ref struct ReadOnlySpan<T>
                        where T : struct
                    {
                    }
                }
                """;
        string sourceB = """
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
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
                    public static MyCollection<T> Create<T>(int arg, ReadOnlySpan<T> items) where T : struct => default;
                }
                """;
        string sourceC = """
                class Program
                {
                    static MyCollection<T> NoArgs<T>() => [];
                    static MyCollection<T> EmptyArgs<T>() => [with()];
                    static MyCollection<T> WithArg<T>(int arg) => [with(arg)];
                    static MyCollection<T> Params<T>(params MyCollection<T> c) => c;
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB, sourceC, CollectionBuilderAttributeDefinition]);
        comp.VerifyEmitDiagnostics(
            // (5,52): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(int, ReadOnlySpan<T>)'
            //     static MyCollection<T> WithArg<T>(int arg) => [with(arg)];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(arg)").WithArguments("MyBuilder.Create<T>(int, System.ReadOnlySpan<T>)", "T", "T").WithLocation(5, 52),
            // (13,61): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'ReadOnlySpan<T>'
            //     public static MyCollection<T> Create<T>(ReadOnlySpan<T> items) => default;
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "items").WithArguments("System.ReadOnlySpan<T>", "T", "T").WithLocation(13, 61));
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
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (12,69): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //     static IMyCollection<T?> F<T>(ReadOnlySpan<T> items, T arg) => [with(arg), ..items];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(arg)").WithArguments("Create", "1").WithLocation(12, 69));
    }

    [Fact]
    public void CollectionBuilder_SpreadElement_BoxingConversion_A()
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
                    public static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => new(items, arg);
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
            expectedOutput: IncludeExpectedOutput("[null], [1, 2, 3], "),
            verify: Verification.FailsPEVerify);
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void CollectionBuilder_UseSiteError_Method()
    {
        // [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
        // public sealed class MyCollection<T>
        // {
        //     public IEnumerator<T> GetEnumerator() { }
        // }
        // public static class MyCollectionBuilder
        // {
        //     [CompilerFeatureRequired("MyFeature")]
        //     public static MyCollection<T> MyCollectionBuilder.Create<T>(ReadOnlySpan<T>, object arg = null) { }
        // }
        string sourceA = """
                .assembly extern System.Runtime { .ver 8:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
                .class public sealed MyCollection`1<T>
                {
                  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CollectionBuilderAttribute::.ctor(class [System.Runtime]System.Type, string) = { type(MyCollectionBuilder) string('Create') }
                  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
                  .method public instance class [System.Runtime]System.Collections.Generic.IEnumerator`1<!T> GetEnumerator() { ldnull ret }
                }
                .class public abstract sealed MyCollectionBuilder
                {
                  .method public static class MyCollection`1<!!T> Create<T>(valuetype [System.Runtime]System.ReadOnlySpan`1<!!T> items, [opt] object arg)
                  {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = { string('MyFeature') }
                    .param [2] = nullref
                    ldnull ret
                  }
                }
                """;
        var refA = CompileIL(sourceA);

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
        var comp = CreateCompilation(sourceB, references: new[] { refA }, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(default)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(default)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (8,19): error CS8716: There is no target type for the default literal.
            //         c = [with(default)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 19),
            // (9,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = F(1, 2);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(1, 2)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 13),
            // (11,33): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> F<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 33));
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
            // (8,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         c = [with(default)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(default)").WithArguments("Create", "1").WithLocation(8, 14));
    }

    [Fact]
    public void CollectionBuilder_ObsoleteBuilderMethod_01_A()
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
                    public static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) => default;
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
            // (8,14): warning CS0612: 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)' is obsolete
            //         c = [with(default)];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(8, 14));
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
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(default)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(default)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (8,19): error CS8716: There is no target type for the default literal.
            //         c = [with(default)];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 19),
            // (9,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = F(1, 2);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(1, 2)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 13),
            // (11,33): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> F<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 33));
    }

    [Fact]
    public void CollectionBuilder_ObsoleteBuilderMethod_02_A()
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
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) => default;
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
            // (6,13): warning CS0612: 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)' is obsolete
            //         c = [];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "[]").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(6, 13),
            // (7,14): warning CS0612: 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)' is obsolete
            //         c = [with()];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with()").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(7, 14),
            // (8,14): warning CS0612: 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)' is obsolete
            //         c = [with(default)];
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "with(default)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(8, 14),
            // (9,13): warning CS0612: 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)' is obsolete
            //         c = F(1, 2);
            Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "F(1, 2)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)").WithLocation(9, 13),
            // (11,33): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> F<T>(params MyCollection<T> c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 33));
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
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "int", "MyCollection").WithLocation(6, 13),
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
            //         c = [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "int", "MyCollection").WithLocation(7, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
            //         c = [with(0)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(0)]").WithArguments("Create", "int", "MyCollection").WithLocation(8, 13),
            // (9,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
            //         c = F(1, 2);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(1, 2)").WithArguments("Create", "int", "MyCollection").WithLocation(9, 13),
            // (11,27): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<int>' and return type 'MyCollection'.
            //     static MyCollection F(params MyCollection c) => c;
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection c").WithArguments("Create", "int", "MyCollection").WithLocation(11, 27),
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (9,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         y = [with(2), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(2)").WithArguments("Create", "1").WithLocation(9, 14));

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
        comp = CreateCompilation(
            [sourceA, sourceB2],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         x = [with(default)];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(default)").WithArguments("Create", "1").WithLocation(6, 14),
            // (7,14): error CS9405: No overload for method 'Create' takes 1 'with(...)' element arguments
            //         x = [with(2), 3];
            Diagnostic(ErrorCode.ERR_BadCollectionArgumentsArgCount, "with(2)").WithArguments("Create", "1").WithLocation(7, 14));
    }

    [Fact]
    public void CollectionBuilder_GenericConstraints_01_A()
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
                    public static MyCollection<T> Create<T>(T arg, ReadOnlySpan<T> items) where T : struct => new(arg, items);
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
            expectedOutput: IncludeExpectedOutput("[null, 1], [2, 3], [null, 1], [0, 3], "),
            verify: Verification.FailsPEVerify);
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
            // (6,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         x = [with(default)];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(default)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(6, 14),
            // (7,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         x = [with(2), 3];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(2)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(7, 14));
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(), 1];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), 1]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (10,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(2)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(2)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 13),
            // (12,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F<int>();
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F<int>()").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 9),
            // (13,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F(3);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(3)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(13, 9),
            // (14,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F(4, 5);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(4, 5)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(14, 9),
            // (16,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c) where T : struct
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(16, 22));

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
        comp = CreateCompilation(
            [sourceA, sourceB2],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(), 1];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), 1]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(2)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(2)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (9,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F<object>();
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F<object>()").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 9),
            // (10,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F((object)3);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F((object)3)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 9),
            // (12,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c)
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 22));
    }

    [Fact]
    public void CollectionBuilder_GenericConstraints_02_A()
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
                    public static MyCollection<T> Create<T>(T arg = default, ReadOnlySpan<T> items = default) where T : struct => new(arg, items);
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (16,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c) where T : struct
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(16, 22));

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
        comp = CreateCompilation(
            [sourceA, sourceB2],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         c = [];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "[]").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(6, 13),
            // (7,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         c = [with(), 1];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with()").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(7, 14),
            // (8,14): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         c = [with(2)];
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "with(2)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(8, 14),
            // (9,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         F<object>();
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<object>()").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(9, 9),
            // (10,9): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'MyBuilder.Create<T>(T, ReadOnlySpan<T>)'
            //         F((object)3);
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F((object)3)").WithArguments("MyBuilder.Create<T>(T, System.ReadOnlySpan<T>)", "T", "object").WithLocation(10, 9),
            // (12,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c)
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 22));
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
        var comp = CreateCompilation(
            [sourceA, sourceB1, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(), 1];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), 1]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (10,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(2, 3)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(2, 3)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 13),
            // (12,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F<int>();
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F<int>()").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 9),
            // (13,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F(4, 5);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F(4, 5)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(13, 9),
            // (15,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c) where T : struct
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(15, 22));

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
        comp = CreateCompilation(
            [sourceA, sourceB2],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (6,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(6, 13),
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(), 1];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), 1]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 13),
            // (8,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(2, 3)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(2, 3)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(8, 13),
            // (9,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F<object>();
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F<object>()").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 9),
            // (10,9): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         F((object)4, 5);
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "F((object)4, 5)").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 9),
            // (12,22): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static void F<T>(params MyCollection<T> c)
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "params MyCollection<T> c").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 22));
    }

    [Fact]
    public void List_NoElements()
    {
        string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Report(ListNoArguments<int>());
                        Report(ListEmptyArguments<int>());
                        Report(ListWithCapacity<int>(16));
                    }
                    static void Report<T>(List<T> list)
                    {
                        Console.WriteLine("Count:{0}, Capacity:{1}", list.Count, list.Capacity);
                    }
                    static List<T> ListNoArguments<T>() => [];
                    static List<T> ListEmptyArguments<T>() => [with()];
                    static List<T> ListWithCapacity<T>(int capacity) => [with(capacity: capacity)];
                }
                """;
        var verifier = CompileAndVerify(
            source,
            expectedOutput: """
                    Count:0, Capacity:0
                    Count:0, Capacity:0
                    Count:0, Capacity:16
                    """);
        verifier.VerifyDiagnostics();
        string expectedILNoArguments = """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
                  IL_0005:  ret
                }
                """;
        verifier.VerifyIL("Program.ListNoArguments<T>", expectedILNoArguments);
        verifier.VerifyIL("Program.ListEmptyArguments<T>", expectedILNoArguments);
        verifier.VerifyIL("Program.ListWithCapacity<T>", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                  IL_0006:  ret
                }
                """);
    }

    [Fact]
    public void List_SingleSpread()
    {
        string source = """
            using System;
            using System.Collections.Generic;
            class Program
            {
                static void Main()
                {
                    Report(ListNoArguments([1, 2]));
                    Report(ListEmptyArguments([3, 4]));
                    Report(ListWithCapacity([5, 6], 16));
                }
                static void Report<T>(List<T> list)
                {
                    list.Report();
                    Console.WriteLine("Capacity:{0}", list.Capacity);
                }
                static List<T> ListNoArguments<T>(IEnumerable<T> e) => [..e];
                static List<T> ListEmptyArguments<T>(IEnumerable<T> e) => [with(), ..e];
                static List<T> ListWithCapacity<T>(IEnumerable<T> e, int capacity) => [with(capacity: capacity), ..e];
            }
            """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            expectedOutput: """
                    [1, 2], Capacity:2
                    [3, 4], Capacity:4
                    [5, 6], Capacity:16
                    """);
        verifier.VerifyDiagnostics();
        string expectedILNoArguments = """
            {
                // Code size        7 (0x7)
                .maxstack  1
                IL_0000:  ldarg.0
                IL_0001:  call       "System.Collections.Generic.List<T> System.Linq.Enumerable.ToList<T>(System.Collections.Generic.IEnumerable<T>)"
                IL_0006:  ret
            }
            """;
        verifier.VerifyIL("Program.ListNoArguments<T>", expectedILNoArguments);
        verifier.VerifyIL("Program.ListEmptyArguments<T>", """
            {
              // Code size       13 (0xd)
              .maxstack  3
              IL_0000:  newobj     "System.Collections.Generic.List<T>..ctor()"
              IL_0005:  dup
              IL_0006:  ldarg.0
              IL_0007:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
              IL_000c:  ret
            }
            """);
        verifier.VerifyIL("Program.ListWithCapacity<T>", """
            {
                // Code size       14 (0xe)
                .maxstack  3
                IL_0000:  ldarg.1
                IL_0001:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                IL_0006:  dup
                IL_0007:  ldarg.0
                IL_0008:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
                IL_000d:  ret
            }
            """);
    }

    [Fact]
    public void CollectionBuilder_SingleSpread()
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
                        _items.AddRange(items.ToArray());
                        _items.AddRange(args);
                    }
                    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, params T[] args) => new(args, items);
                }
                """;
        string sourceB = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        NoArguments([1, 2]).Report();
                        EmptyArguments([3, 4]).Report();
                        WithArguments([5, 6], 7).Report();
                    }
                    static MyCollection<T> NoArguments<T>(ReadOnlySpan<T> s) => [..s];
                    static MyCollection<T> EmptyArguments<T>(ReadOnlySpan<T> s) => [with(), ..s];
                    static MyCollection<T> WithArguments<T>(ReadOnlySpan<T> s, params T[] args) => [with(args), ..s];
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (10,65): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> NoArguments<T>(ReadOnlySpan<T> s) => [..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[..s]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(10, 65),
            // (11,68): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> EmptyArguments<T>(ReadOnlySpan<T> s) => [with(), ..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), ..s]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(11, 68),
            // (12,84): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //     static MyCollection<T> WithArguments<T>(ReadOnlySpan<T> s, params T[] args) => [with(args), ..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(args), ..s]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(12, 84));
    }

    [Fact]
    public void ImmutableArray_NoElements()
    {
        string sourceA = """
                #pragma warning disable 436 // type conflicts with imported type
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                namespace System.Collections.Immutable
                {
                    [CollectionBuilder(typeof(MyBuilder), "Create")]
                    public struct ImmutableArray<T> : IEnumerable<T>
                    {
                        public static readonly ImmutableArray<T> Empty = new(default, new T[0]);
                        private readonly List<T> _items;
                        internal ImmutableArray(ReadOnlySpan<T> items, T[] args)
                        {
                            _items = new();
                            _items.AddRange(items.ToArray());
                            _items.AddRange(args);
                        }
                        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public static class MyBuilder
                    {
                        public static ImmutableArray<T> Create<T>(ReadOnlySpan<T> items, params T[] args) => new(items, args);
                    }
                }
                """;
        string sourceB = """
                #pragma warning disable 436 // type conflicts with imported type
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArrayNoArguments<int>().Report();
                        ImmutableArrayEmptyArguments<int>().Report();
                        ImmutableArrayWithArguments(5, 6).Report();
                    }
                    static ImmutableArray<T> ImmutableArrayNoArguments<T>() => [];
                    static ImmutableArray<T> ImmutableArrayEmptyArguments<T>() => [with()];
                    static ImmutableArray<T> ImmutableArrayWithArguments<T>(params T[] args) => [with(args)];
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (11,64): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayNoArguments<T>() => [];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(11, 64),
            // (12,67): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayEmptyArguments<T>() => [with()];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with()]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(12, 67),
            // (13,81): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayWithArguments<T>(params T[] args) => [with(args)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(args)]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(13, 81));
    }

    [Fact]
    public void ImmutableArray_SingleSpread()
    {
        string sourceA = """
                #pragma warning disable 436 // type conflicts with imported type
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                namespace System.Collections.Immutable
                {
                    [CollectionBuilder(typeof(MyBuilder), "Create")]
                    public struct ImmutableArray<T> : IEnumerable<T>
                    {
                        private readonly List<T> _items;
                        internal ImmutableArray(ReadOnlySpan<T> items, T[] args)
                        {
                            _items = new();
                            _items.AddRange(items.ToArray());
                            _items.AddRange(args);
                        }
                        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public static class MyBuilder
                    {
                        public static ImmutableArray<T> Create<T>(ReadOnlySpan<T> items, params T[] args) => new(items, args);
                    }
                }
                """;
        string sourceB = """
                #pragma warning disable 436 // type conflicts with imported type
                using System;
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArrayNoArguments([1, 2]).Report();
                        ImmutableArrayEmptyArguments([3, 4]).Report();
                        ImmutableArrayWithArguments([5, 6], 7).Report();
                    }
                    static ImmutableArray<T> ImmutableArrayNoArguments<T>(ReadOnlySpan<T> s) => [..s];
                    static ImmutableArray<T> ImmutableArrayEmptyArguments<T>(ReadOnlySpan<T> s) => [with(), ..s];
                    static ImmutableArray<T> ImmutableArrayWithArguments<T>(ReadOnlySpan<T> s, params T[] args) => [with(args), ..s];
                }
                """;
        var comp = CreateCompilation(
            [sourceA, sourceB, s_collectionExtensions],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (12,81): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayNoArguments<T>(ReadOnlySpan<T> s) => [..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[..s]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(12, 81),
            // (13,84): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayEmptyArguments<T>(ReadOnlySpan<T> s) => [with(), ..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(), ..s]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(13, 84),
            // (14,100): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'ImmutableArray<T>'.
            //     static ImmutableArray<T> ImmutableArrayWithArguments<T>(ReadOnlySpan<T> s, params T[] args) => [with(args), ..s];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(args), ..s]").WithArguments("Create", "T", "System.Collections.Immutable.ImmutableArray<T>").WithLocation(14, 100));
    }

    [Theory, CombinatorialData]
    public void RefSafety_ConstructorArguments(bool scopedInParameter, bool scopedOutArgument)
    {
        string sourceA = $$"""
                using System.Collections;
                using System.Collections.Generic;
                ref struct R<T>
                {
                    public R(ref T t) { }
                }
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection() { }
                    public MyCollection({{(scopedInParameter ? "scoped" : "")}} R<T> a, out R<T> b) { b = default; }
                    public void Add(T t) { }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                """;
        string sourceB = $$"""
                class Program
                {
                    static void F<T>(T x, T y)
                    {
                        MyCollection<T> c;
                        T t = default;
                        R<T> a = new(ref t);
                        {{(scopedOutArgument ? "scoped" : "")}} R<T> b;
                        c = new(a, out b);
                        c = [with(a, out b), x, y];
                    }
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB]);
        if (scopedInParameter || scopedOutArgument)
        {
            comp.VerifyEmitDiagnostics();
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (9,13): error CS8350: This combination of arguments to 'MyCollection<T>.MyCollection(R<T>, out R<T>)' is disallowed because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         c = new(a, out b);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "new(a, out b)").WithArguments("MyCollection<T>.MyCollection(R<T>, out R<T>)", "a").WithLocation(9, 13),
                // (9,17): error CS8352: Cannot use variable 'a' in this context because it may expose referenced variables outside of their declaration scope
                //         c = new(a, out b);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "a").WithArguments("a").WithLocation(9, 17),
                // (10,14): error CS8350: This combination of arguments to 'MyCollection<T>.MyCollection(R<T>, out R<T>)' is disallowed because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         c = [with(a, out b), x, y];
                Diagnostic(ErrorCode.ERR_CallArgMixing, "with(a, out b)").WithArguments("MyCollection<T>.MyCollection(R<T>, out R<T>)", "a").WithLocation(10, 14),
                // (10,19): error CS8352: Cannot use variable 'a' in this context because it may expose referenced variables outside of their declaration scope
                //         c = [with(a, out b), x, y];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "a").WithArguments("a").WithLocation(10, 19));
        }
    }

    [Theory]
    [CombinatorialData]
    public void RefSafety_CollectionBuilderArguments(bool scopedInParameter, bool scopedOutArgument)
    {
        string sourceA = $$"""
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
                    public static MyCollection<T> Create<T>({{(scopedInParameter ? "scoped" : "")}} ReadOnlySpan<T> items, out ReadOnlySpan<T> other)
                    {
                        other = default;
                        return default;
                    }
                }
                """;
        string sourceB = $$"""
                using System;
                class Program
                {
                    static void F<T>(T x, T y)
                    {
                        MyCollection<T> c;
                        {{(scopedOutArgument ? "scoped" : "")}} ReadOnlySpan<T> s;
                        c = MyBuilder.Create([x, y], out s);
                        c = [with(out s), x, y];
                    }
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB], targetFramework: TargetFramework.Net80);
        if (scopedInParameter || scopedOutArgument)
        {
            comp.VerifyEmitDiagnostics(
                // (9,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         c = [with(out s), x, y];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(out s), x, y]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 13));
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (8,13): error CS8350: This combination of arguments to 'MyBuilder.Create<T>(ReadOnlySpan<T>, out ReadOnlySpan<T>)' is disallowed because it may expose variables referenced by parameter 'items' outside of their declaration scope
                //         c = MyBuilder.Create([x, y], out s);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "MyBuilder.Create([x, y], out s)").WithArguments("MyBuilder.Create<T>(System.ReadOnlySpan<T>, out System.ReadOnlySpan<T>)", "items").WithLocation(8, 13),
                // (8,30): error CS9203: A collection expression of type 'ReadOnlySpan<T>' cannot be used in this context because it may be exposed outside of the current scope.
                //         c = MyBuilder.Create([x, y], out s);
                Diagnostic(ErrorCode.ERR_CollectionExpressionEscape, "[x, y]").WithArguments("System.ReadOnlySpan<T>").WithLocation(8, 30),
                // (9,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
                //         c = [with(out s), x, y];
                Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(out s), x, y]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(9, 13));
        }
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
            // (7,14): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
            //         x = [with(t), t];
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "with(t)").WithArguments("U").WithLocation(7, 14),
            // (8,17): error CS9400: 'with(...)' element must be the first element
            //         x = [t, with(t)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 17),
            // (14,14): error CS0417: 'U': cannot provide arguments when creating an instance of a variable type
            //         y = [with(t), t];
            Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "with(t)").WithArguments("U").WithLocation(14, 14),
            // (15,17): error CS9400: 'with(...)' element must be the first element
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
    public void EvaluationOrder_CollectionInitializer()
    {
        string sourceA = """
                using System;
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
                """;
        string sourceB = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection(A x = null, A y = null) { Console.WriteLine("MyCollection({0}, {1})", x, y); }
                    public void Add(T t) { Console.WriteLine("Add({0})", t); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                """;
        string sourceC = """
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
            [sourceA, sourceB, sourceC],
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
    public void EvaluationOrder_CollectionBuilder()
    {
        string sourceA = """
                using System;
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
                """;
        string sourceB = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyBuilder), "Create")]
                class MyCollection<T> : IEnumerable<T>
                {
                    public MyCollection(ReadOnlySpan<T> items, A x, A y)
                    {
                        Console.WriteLine("MyCollection({0}, {1})", x, y); 
                        Console.WriteLine(items.Length);
                    }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
                class MyBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items, A x = null, A y = null) => new(items, x, y);
                }
                """;
        string sourceC = """
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

        var comp = CreateCompilation(
            [sourceA, sourceB, sourceC],
            targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (7,13): error CS9187: Could not find an accessible 'Create' method with the expected signature: a static method with a single parameter of type 'ReadOnlySpan<T>' and return type 'MyCollection<T>'.
            //         c = [with(y: Identity(1), x: Identity(2)), Identity(3), Identity(4)];
            Diagnostic(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, "[with(y: Identity(1), x: Identity(2)), Identity(3), Identity(4)]").WithArguments("Create", "T", "MyCollection<T>").WithLocation(7, 13));
    }

    [Fact]
    public void EvaluationOrder_CollectionBuilder_A()
    {
        string sourceA = """
            using System;
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
            """;
        string sourceB = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items, A x, A y)
                {
                    Console.WriteLine("MyCollection({0}, {1})", x, y); 
                    Console.WriteLine(items.Length);
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(A x = null, A y = null, ReadOnlySpan<T> items = default) => new(items, x, y);
            }
            """;
        string sourceC = """
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
        var comp = CompileAndVerify(
            [sourceA, sourceB, sourceC],
            targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput("""
                1
                1 -> A
                2
                2 -> A
                3
                3 -> A
                4
                4 -> A
                MyCollection(2, 1)
                2
                """),
            verify: Verification.Fails).VerifyIL("Program.Main", """
            {
              // Code size       87 (0x57)
              .maxstack  4
              .locals init (<>y__InlineArray2<A> V_0,
                            A V_1)
              IL_0000:  ldc.i4.1
              IL_0001:  call       "int Program.Identity<int>(int)"
              IL_0006:  call       "A A.op_Implicit(int)"
              IL_000b:  stloc.1
              IL_000c:  ldc.i4.2
              IL_000d:  call       "int Program.Identity<int>(int)"
              IL_0012:  call       "A A.op_Implicit(int)"
              IL_0017:  ldloc.1
              IL_0018:  ldloca.s   V_0
              IL_001a:  initobj    "<>y__InlineArray2<A>"
              IL_0020:  ldloca.s   V_0
              IL_0022:  ldc.i4.0
              IL_0023:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_0028:  ldc.i4.3
              IL_0029:  call       "int Program.Identity<int>(int)"
              IL_002e:  call       "A A.op_Implicit(int)"
              IL_0033:  stind.ref
              IL_0034:  ldloca.s   V_0
              IL_0036:  ldc.i4.1
              IL_0037:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_003c:  ldc.i4.4
              IL_003d:  call       "int Program.Identity<int>(int)"
              IL_0042:  call       "A A.op_Implicit(int)"
              IL_0047:  stind.ref
              IL_0048:  ldloca.s   V_0
              IL_004a:  ldc.i4.2
              IL_004b:  call       "System.ReadOnlySpan<A> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<A>, A>(in <>y__InlineArray2<A>, int)"
              IL_0050:  call       "MyCollection<A> MyBuilder.Create<A>(A, A, System.ReadOnlySpan<A>)"
              IL_0055:  pop
              IL_0056:  ret
            }
            """);
    }

    [Fact]
    public void EvaluationOrder_CollectionBuilder_B()
    {
        string sourceA = """
            using System;
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
            """;
        string sourceB = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items, A x, A y)
                {
                    Console.WriteLine("MyCollection({0}, {1})", x, y); 
                    Console.WriteLine(items.Length);
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(A x = null, A y = null, ReadOnlySpan<T> items = default) => new(items, x, y);
            }
            """;
        string sourceC = """
            using System;
            class Program
            {
                static void Main()
                {
                    MyCollection<A> c;
                    c = [with(y: Identity(1)), Identity(3), Identity(4)];
                }
                static T Identity<T>(T value)
                {
                    Console.WriteLine(value);
                    return value;
                }
            }
            """;
        var comp = CompileAndVerify(
            [sourceA, sourceB, sourceC],
            targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput("""
                1
                1 -> A
                3
                3 -> A
                4
                4 -> A
                MyCollection(, 1)
                2
                """),
            verify: Verification.Fails).VerifyIL("Program.Main", """
            {
              // Code size       75 (0x4b)
              .maxstack  4
              .locals init (<>y__InlineArray2<A> V_0)
              IL_0000:  ldnull
              IL_0001:  ldc.i4.1
              IL_0002:  call       "int Program.Identity<int>(int)"
              IL_0007:  call       "A A.op_Implicit(int)"
              IL_000c:  ldloca.s   V_0
              IL_000e:  initobj    "<>y__InlineArray2<A>"
              IL_0014:  ldloca.s   V_0
              IL_0016:  ldc.i4.0
              IL_0017:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_001c:  ldc.i4.3
              IL_001d:  call       "int Program.Identity<int>(int)"
              IL_0022:  call       "A A.op_Implicit(int)"
              IL_0027:  stind.ref
              IL_0028:  ldloca.s   V_0
              IL_002a:  ldc.i4.1
              IL_002b:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_0030:  ldc.i4.4
              IL_0031:  call       "int Program.Identity<int>(int)"
              IL_0036:  call       "A A.op_Implicit(int)"
              IL_003b:  stind.ref
              IL_003c:  ldloca.s   V_0
              IL_003e:  ldc.i4.2
              IL_003f:  call       "System.ReadOnlySpan<A> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<A>, A>(in <>y__InlineArray2<A>, int)"
              IL_0044:  call       "MyCollection<A> MyBuilder.Create<A>(A, A, System.ReadOnlySpan<A>)"
              IL_0049:  pop
              IL_004a:  ret
            }
            """);
    }

    [Fact]
    public void EvaluationOrder_CollectionBuilder_C()
    {
        string sourceA = """
            using System;
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
            """;
        string sourceB = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items, A x, A y) {
                    Console.WriteLine("MyCollection({0}, {1})", x, y); 
                    Console.WriteLine(items.Length);
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(A x = null, A y = null, ReadOnlySpan<T> items = default) => new(items, x, y);
            }
            """;
        string sourceC = """
            using System;
            class Program
            {
                static void Main()
                {
                    MyCollection<A> c;
                    c = [with(), Identity(3), Identity(4)];
                }
                static T Identity<T>(T value)
                {
                    Console.WriteLine(value);
                    return value;
                }
            }
            """;
        var comp = CompileAndVerify(
            [sourceA, sourceB, sourceC],
            targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput("""
                3
                3 -> A
                4
                4 -> A
                MyCollection(, )
                2
                """),
            verify: Verification.Fails).VerifyIL("Program.Main", """
            {
              // Code size       65 (0x41)
              .maxstack  4
              .locals init (<>y__InlineArray2<A> V_0)
              IL_0000:  ldnull
              IL_0001:  ldnull
              IL_0002:  ldloca.s   V_0
              IL_0004:  initobj    "<>y__InlineArray2<A>"
              IL_000a:  ldloca.s   V_0
              IL_000c:  ldc.i4.0
              IL_000d:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_0012:  ldc.i4.3
              IL_0013:  call       "int Program.Identity<int>(int)"
              IL_0018:  call       "A A.op_Implicit(int)"
              IL_001d:  stind.ref
              IL_001e:  ldloca.s   V_0
              IL_0020:  ldc.i4.1
              IL_0021:  call       "ref A <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<A>, A>(ref <>y__InlineArray2<A>, int)"
              IL_0026:  ldc.i4.4
              IL_0027:  call       "int Program.Identity<int>(int)"
              IL_002c:  call       "A A.op_Implicit(int)"
              IL_0031:  stind.ref
              IL_0032:  ldloca.s   V_0
              IL_0034:  ldc.i4.2
              IL_0035:  call       "System.ReadOnlySpan<A> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<A>, A>(in <>y__InlineArray2<A>, int)"
              IL_003a:  call       "MyCollection<A> MyBuilder.Create<A>(A, A, System.ReadOnlySpan<A>)"
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
            // (8,14): error CS7036: There is no argument given that corresponds to the required parameter '__arglist' of 'MyCollection.MyCollection(__arglist)'
            //         c = [with()];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with()").WithArguments("__arglist", "MyCollection.MyCollection(__arglist)").WithLocation(8, 14));
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
            // (9,29): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
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
            // (10,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with((dynamic)1)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)1").WithLocation(10, 19),
            // (11,22): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with(y: (dynamic)"2")];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, @"(dynamic)""2""").WithLocation(11, 22),
            // (12,17): error CS9501: Collection argument element must be the first element.
            //         c = [3, with(1, (dynamic)"2")];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(12, 17),
            // (12,25): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [3, with(1, (dynamic)"2")];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, @"(dynamic)""2""").WithLocation(12, 25),
            // (13,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with((dynamic)1, (dynamic)"2"), 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)1").WithLocation(13, 19),
            // (13,31): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with((dynamic)1, (dynamic)"2"), 3];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, @"(dynamic)""2""").WithLocation(13, 31),
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
            // (19,23): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
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
            // (12,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with((dynamic)null)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)null").WithLocation(12, 19),
            // (13,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         c = [with((dynamic)0)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)0").WithLocation(13, 19),
            // (15,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
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
            // (7,19): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         a = [with((dynamic)null), null];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "(dynamic)null").WithLocation(7, 19));
    }

    [Fact]
    public void TypeInference_List()
    {
        string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Identity([with()]);
                        Identity([with(capacity: 1), default]);
                        Identity([with(capacity: 1), 3]);
                        Identity([with(collection: [default, 2]), default]);
                        Identity([with(collection: [default]), default, 3]);
                    }
                    static List<T> Identity<T>(List<T> c) => c;
                }
                """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (6,9): error CS0411: The type arguments for method 'Program.Identity<T>(List<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with()]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(System.Collections.Generic.List<T>)").WithLocation(6, 9),
            // (7,9): error CS0411: The type arguments for method 'Program.Identity<T>(List<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with(capacity: 1), default]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(System.Collections.Generic.List<T>)").WithLocation(7, 9),
            // (9,9): error CS0411: The type arguments for method 'Program.Identity<T>(List<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with(collection: [default, 2]), default]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(System.Collections.Generic.List<T>)").WithLocation(9, 9));
    }

    [Fact]
    public void TypeInference_CollectionInitializer()
    {
        string sourceA = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                class MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _items;
                    public MyCollection(params T[] args) { _items = new(args); }
                    public void Add(T t) { _items.Add(t); }
                    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
                }
                """;
        string sourceB = """
                class Program
                {
                    static void Main()
                    {
                        Identity([with()]);
                        Identity([with(default, 2), default]);
                        Identity([with(default), default, 3]);
                    }
                    static MyCollection<T> Identity<T>(MyCollection<T> c) => c;
                }
                """;
        var comp = CreateCompilation([sourceA, sourceB]);
        comp.VerifyEmitDiagnostics(
            // (5,9): error CS0411: The type arguments for method 'Program.Identity<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with()]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(MyCollection<T>)").WithLocation(5, 9),
            // (6,9): error CS0411: The type arguments for method 'Program.Identity<T>(MyCollection<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         Identity([with(default, 2), default]);
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Identity").WithArguments("Program.Identity<T>(MyCollection<T>)").WithLocation(6, 9));
    }

#if DICTIONARY_EXPRESSIONS

    [Fact]
    public void InterfaceTarget_ReorderedArguments()
    {
        string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create<int, string>(EqualityComparer<int>.Default, 2, new(1, "one")).Report();
                    }
                    static IDictionary<K, V> Create<K, V>(IEqualityComparer<K> e, int c, KeyValuePair<K, V> x)
                    {
                        return [with(comparer: Identity(e), capacity: Identity(c)), Identity(x)];
                    }
                    static T Identity<T>(T value)
                    {
                        Console.WriteLine(value);
                        return value;
                    }
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
                expectedOutput: """
                        System.Collections.Generic.GenericEqualityComparer`1[System.Int32]
                        2
                        [1, one]
                        [[1, one]],
                        """);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.Create<K, V>", """
                {
                  // Code size       47 (0x2f)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0,
                                System.Collections.Generic.IEqualityComparer<K> V_1)
                  IL_0000:  ldarg.0
                  IL_0001:  call       "System.Collections.Generic.IEqualityComparer<K> Program.Identity<System.Collections.Generic.IEqualityComparer<K>>(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0006:  stloc.1
                  IL_0007:  ldarg.1
                  IL_0008:  call       "int Program.Identity<int>(int)"
                  IL_000d:  ldloc.1
                  IL_000e:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(int, System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0013:  ldarg.2
                  IL_0014:  call       "System.Collections.Generic.KeyValuePair<K, V> Program.Identity<System.Collections.Generic.KeyValuePair<K, V>>(System.Collections.Generic.KeyValuePair<K, V>)"
                  IL_0019:  stloc.0
                  IL_001a:  dup
                  IL_001b:  ldloca.s   V_0
                  IL_001d:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0022:  ldloca.s   V_0
                  IL_0024:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_0029:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_002e:  ret
                }
                """);
    }

    [Fact]
    public void InterfaceTarget_Dynamic()
    {
        string source = """
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        CreateReadOnlyDictionary(null, 1, "one");
                        CreateDictionary(2, null, 2, "two");
                    }
                    static IReadOnlyDictionary<K, V> CreateReadOnlyDictionary<K, V>(dynamic d, K k, V v)
                    {
                        return [with(d), k:v];
                    }
                    static IDictionary<K, V> CreateDictionary<K, V>(dynamic x, dynamic y, K k, V v)
                    {
                        return [with(x, y), k:v];
                    }
                }
                """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
        comp.VerifyEmitDiagnostics(
            // (11,22): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         return [with(d), k:v];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "d").WithLocation(11, 22),
            // (15,22): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         return [with(x, y), k:v];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "x").WithLocation(15, 22),
            // (15,25): error CS9503: Collection arguments cannot be dynamic; compile-time binding is required.
            //         return [with(x, y), k:v];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsDynamicBinding, "y").WithLocation(15, 25));
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_FieldInitializer(
        [CombinatorialValues("IDictionary", "IReadOnlyDictionary")] string typeName)
    {
        string source = $$"""
                using System.Collections.Generic;
                class C<K, V>
                {
                    public {{typeName}}<K, V> F =
                        [with(GetComparer())];
                    static IEqualityComparer<K> GetComparer() => null;
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C<int, string>();
                        c.F.Report();
                    }
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            expectedOutput: "[], ");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C<K, V>..ctor",
            typeName == "IDictionary" ?
            """
                {
                  // Code size       23 (0x17)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  call       "System.Collections.Generic.IEqualityComparer<K> C<K, V>.GetComparer()"
                  IL_0006:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_000b:  stfld      "System.Collections.Generic.IDictionary<K, V> C<K, V>.F"
                  IL_0010:  ldarg.0
                  IL_0011:  call       "object..ctor()"
                  IL_0016:  ret
                }
                """ :
            """
                {
                  // Code size       28 (0x1c)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  call       "System.Collections.Generic.IEqualityComparer<K> C<K, V>.GetComparer()"
                  IL_0006:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_000b:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0010:  stfld      "System.Collections.Generic.IReadOnlyDictionary<K, V> C<K, V>.F"
                  IL_0015:  ldarg.0
                  IL_0016:  call       "object..ctor()"
                  IL_001b:  ret
                }
                """);
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_Nullability(
        [CombinatorialValues("IDictionary", "IReadOnlyDictionary")] string typeName)
    {
        string source = $$"""
#nullable enable
                using System.Collections.Generic;
                class Program
                {
                    static {{typeName}}<K, V> Create1<K, V>(bool b, IEqualityComparer<K>? c1)
                    {
                        if (b) return new Dictionary<K, V>(c1);
                        return [with(c1)];
                    }
                    static {{typeName}}<K, V> Create2<K, V>(bool b, IEqualityComparer<K?> c2) where K : class
                    {
                        if (b) return new Dictionary<K, V>(c2);
                        return [with(c2)];
                    }
                    static {{typeName}}<K?, V> Create3<K, V>(bool b, IEqualityComparer<K> c3) where K : class
                    {
                        if (b) return new Dictionary<K?, V>(c3);
                        return [with(c3)];
                    }
                }
                """;
        var comp = CreateCompilation(source);
        // PROTOTYPE: Handle collection arguments in flow analysis: report CS8620 for 'with(c3)'.
        comp.VerifyEmitDiagnostics(
            // (17,45): warning CS8620: Argument of type 'IEqualityComparer<K>' cannot be used for parameter 'comparer' of type 'IEqualityComparer<K?>' in 'Dictionary<K?, V>.Dictionary(IEqualityComparer<K?> comparer)' due to differences in the nullability of reference types.
            //         if (b) return new Dictionary<K?, V>(c3);
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "c3").WithArguments("System.Collections.Generic.IEqualityComparer<K>", "System.Collections.Generic.IEqualityComparer<K?>", "comparer", "Dictionary<K?, V>.Dictionary(IEqualityComparer<K?> comparer)").WithLocation(17, 45));
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_GenericMethod(
        [CombinatorialValues("IDictionary", "IReadOnlyDictionary")] string typeName)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static {{typeName}}<K, V> Create<K, V>(IEqualityComparer<K> e)
                    {
                        return [with(e)];
                    }
                    static void Main()
                    {
                        Create<int, string>(null).Report();
                    }
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            expectedOutput: "[], ");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.Create<K, V>",
            typeName == "IDictionary" ?
            """
                {
                    // Code size        7 (0x7)
                    .maxstack  1
                    IL_0000:  ldarg.0
                    IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                    IL_0006:  ret
                }
                """ :
            """
                {
                    // Code size       12 (0xc)
                    .maxstack  1
                    IL_0000:  ldarg.0
                    IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                    IL_0006:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                    IL_000b:  ret
                }
                """);
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_DictionaryInterfaces(
        [CombinatorialValues("IDictionary", "IReadOnlyDictionary")] string typeName)
    {
        string sourceA = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create<int, string>(1, "one", new(2, "two")).Report();
                    }
                    static {{typeName}}<K, V> Create<K, V>(K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(), k:v, x];
                    }
                }
                """;
        var comp = CreateCompilation(
            [sourceA, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        var verifier = CompileAndVerify(
            comp,
            expectedOutput: "[[1, one], [2, two]], ");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Program.Create<K, V>", (typeName is "IDictionary") ?
            """
                {
                  // Code size       36 (0x24)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldarg.0
                  IL_0007:  ldarg.1
                  IL_0008:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000d:  ldarg.2
                  IL_000e:  stloc.0
                  IL_000f:  dup
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0017:  ldloca.s   V_0
                  IL_0019:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_001e:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0023:  ret
                }
                """ :
            """
                {
                  // Code size       41 (0x29)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldarg.0
                  IL_0007:  ldarg.1
                  IL_0008:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000d:  ldarg.2
                  IL_000e:  stloc.0
                  IL_000f:  dup
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0017:  ldloca.s   V_0
                  IL_0019:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_001e:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0023:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0028:  ret
                }
                """);

        string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create1<int, string>(3, 1, "one", new(2, "two")).Report();
                        Create2<int, string>(1, 3, "three", new(4, "four")).Report();
                    }
                    static {{typeName}}<K, V> Create1<K, V>(int c, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(c), k:v, x];
                    }
                    static {{typeName}}<K, V> Create2<K, V>(int c, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(capacity: c), k:v, x];
                    }
                }
                """;
        comp = CreateCompilation(
            [sourceB, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        string expectedIL;
        if (typeName is "IDictionary")
        {
            verifier = CompileAndVerify(
                comp,
                expectedOutput: "[[1, one], [2, two]], [[3, three], [4, four]], ");
            verifier.VerifyDiagnostics();
            expectedIL = """
                    {
                      // Code size       37 (0x25)
                      .maxstack  4
                      .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                      IL_0000:  ldarg.0
                      IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(int)"
                      IL_0006:  dup
                      IL_0007:  ldarg.1
                      IL_0008:  ldarg.2
                      IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_000e:  ldarg.3
                      IL_000f:  stloc.0
                      IL_0010:  dup
                      IL_0011:  ldloca.s   V_0
                      IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_0018:  ldloca.s   V_0
                      IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_0024:  ret
                    }
                    """;
            verifier.VerifyIL("Program.Create1<K, V>", expectedIL);
            verifier.VerifyIL("Program.Create2<K, V>", expectedIL);
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (11,22): error CS1503: Argument 1: cannot convert from 'int' to 'System.Collections.Generic.IEqualityComparer<K>?'
                //         return [with(c), k:v, x];
                Diagnostic(ErrorCode.ERR_BadArgType, "c").WithArguments("1", "int", "System.Collections.Generic.IEqualityComparer<K>?").WithLocation(11, 22),
                // (15,22): error CS1739: The best overload for '<signature>' does not have a parameter named 'capacity'
                //         return [with(capacity: c), k:v, x];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("<signature>", "capacity").WithLocation(15, 22));
        }

        string sourceC = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create1<int, string>(null, 1, "one", new(2, "two")).Report();
                        Create2<int, string>(null, 3, "three", new(4, "four")).Report();
                    }
                    static {{typeName}}<K, V> Create1<K, V>(IEqualityComparer<K> e, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(e), k:v, x];
                    }
                    static {{typeName}}<K, V> Create2<K, V>(IEqualityComparer<K> e, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(comparer: e), k:v, x];
                    }
                }
                """;
        comp = CreateCompilation(
            [sourceC, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        verifier = CompileAndVerify(
            comp,
            expectedOutput: "[[1, one], [2, two]], [[3, three], [4, four]], ");
        verifier.VerifyDiagnostics();
        expectedIL = (typeName is "IDictionary") ?
            """
                {
                  // Code size       37 (0x25)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0006:  dup
                  IL_0007:  ldarg.1
                  IL_0008:  ldarg.2
                  IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000e:  ldarg.3
                  IL_000f:  stloc.0
                  IL_0010:  dup
                  IL_0011:  ldloca.s   V_0
                  IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0018:  ldloca.s   V_0
                  IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0024:  ret
                }
                """ :
            """
                {
                  // Code size       42 (0x2a)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0006:  dup
                  IL_0007:  ldarg.1
                  IL_0008:  ldarg.2
                  IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000e:  ldarg.3
                  IL_000f:  stloc.0
                  IL_0010:  dup
                  IL_0011:  ldloca.s   V_0
                  IL_0013:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0018:  ldloca.s   V_0
                  IL_001a:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_001f:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_0024:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0029:  ret
                }
                """;
        verifier.VerifyIL("Program.Create1<K, V>", expectedIL);
        verifier.VerifyIL("Program.Create2<K, V>", expectedIL);

        string sourceD = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create1<int, string>(null, 3, 1, "one", new(2, "two")).Report();
                        Create2<int, string>(null, 1, 3, "three", new(4, "four")).Report();
                    }
                    static {{typeName}}<K, V> Create1<K, V>(IEqualityComparer<K> e, int c, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(c, e), k:v, x];
                    }
                    static {{typeName}}<K, V> Create2<K, V>(IEqualityComparer<K> e, int c, K k, V v, KeyValuePair<K, V> x)
                    {
                        return [with(capacity: c, comparer: e), k:v, x];
                    }
                }
                """;
        comp = CreateCompilation(
            [sourceD, s_collectionExtensions],
            options: TestOptions.ReleaseExe);
        if (typeName is "IDictionary")
        {
            verifier = CompileAndVerify(
                comp,
                expectedOutput: "[[1, one], [2, two]], [[3, three], [4, four]], ");
            verifier.VerifyDiagnostics();
            expectedIL = """
                    {
                      // Code size       39 (0x27)
                      .maxstack  4
                      .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                      IL_0000:  ldarg.1
                      IL_0001:  ldarg.0
                      IL_0002:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(int, System.Collections.Generic.IEqualityComparer<K>)"
                      IL_0007:  dup
                      IL_0008:  ldarg.2
                      IL_0009:  ldarg.3
                      IL_000a:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_000f:  ldarg.s    V_4
                      IL_0011:  stloc.0
                      IL_0012:  dup
                      IL_0013:  ldloca.s   V_0
                      IL_0015:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                      IL_001a:  ldloca.s   V_0
                      IL_001c:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                      IL_0021:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                      IL_0026:  ret
                    }
                    """;
            verifier.VerifyIL("Program.Create1<K, V>", expectedIL);
            verifier.VerifyIL("Program.Create2<K, V>", expectedIL);
        }
        else
        {
            comp.VerifyEmitDiagnostics(
                // (11,16): error CS1501: No overload for method '<signature>' takes 2 arguments
                //         return [with(c, e), k:v, x];
                Diagnostic(ErrorCode.ERR_BadArgCount, "[with(c, e), k:v, x]").WithArguments("<signature>", "2").WithLocation(11, 16),
                // (15,22): error CS1739: The best overload for '<signature>' does not have a parameter named 'capacity'
                //         return [with(capacity: c, comparer: e), k:v, x];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "capacity").WithArguments("<signature>", "capacity").WithLocation(15, 22));
        }

        string sourceE = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static {{typeName}}<K, V> Create1<K, V>(IEnumerable<KeyValuePair<K, V>> c, K k, V v)
                    {
                        return [with(c), k:v];
                    }
                    static {{typeName}}<K, V> Create2<K, V>(IEnumerable<KeyValuePair<K, V>> c, K k, V v)
                    {
                        return [with(collection: c), k:v];
                    }
                }
                """;
        comp = CreateCompilation(sourceE);
        comp.VerifyEmitDiagnostics(
            // (6,22): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>' to 'System.Collections.Generic.IEqualityComparer<K>?'
            //         return [with(c), k:v];
            Diagnostic(ErrorCode.ERR_BadArgType, "c").WithArguments("1", "System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>", "System.Collections.Generic.IEqualityComparer<K>?").WithLocation(6, 22),
            // (10,22): error CS1739: The best overload for '<signature>' does not have a parameter named 'collection'
            //         return [with(collection: c), k:v];
            Diagnostic(ErrorCode.ERR_BadNamedArgument, "collection").WithArguments("<signature>", "collection").WithLocation(10, 22));
    }


    [Theory]
    [CombinatorialData]
    public void CollectionArguments_CapacityAndComparer_02(
        [CombinatorialValues(
                "System.Collections.Generic.IReadOnlyDictionary<K, V>",
                "System.Collections.Generic.IDictionary<K, V>")]
            string typeName)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Create<K, V>(int capacity, IEqualityComparer<K> comparer)
                    {
                        {{typeName}} c;
                        c = [];
                        c = [with()];
                        c = [with(default)];
                        c = [with(capacity)];
                        c = [with(comparer)];
                        c = [with(capacity, comparer)];
                    }
                }
                """;
        var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
        switch (typeName)
        {
            case "System.Collections.Generic.IReadOnlyDictionary<K, V>":
                comp.VerifyEmitDiagnostics(
                    // (10,19): error CS1503: Argument 1: cannot convert from 'int' to 'System.Collections.Generic.IEqualityComparer<K>?'
                    //         c = [with(capacity)];
                    Diagnostic(ErrorCode.ERR_BadArgType, "capacity").WithArguments("1", "int", "System.Collections.Generic.IEqualityComparer<K>?").WithLocation(10, 19),
                    // (12,13): error CS1501: No overload for method '<signature>' takes 2 arguments
                    //         c = [with(capacity, comparer)];
                    Diagnostic(ErrorCode.ERR_BadArgCount, "[with(capacity, comparer)]").WithArguments("<signature>", "2").WithLocation(12, 13));
                break;
            case "System.Collections.Generic.IDictionary<K, V>":
                comp.VerifyEmitDiagnostics(
                    // (9,13): error CS0121: The call is ambiguous between the following methods or properties: 'Program.<signature>(IEqualityComparer<K>?)' and 'Program.<signature>(int)'
                    //         c = [with(default)];
                    Diagnostic(ErrorCode.ERR_AmbigCall, "[with(default)]").WithArguments("Program.<signature>(System.Collections.Generic.IEqualityComparer<K>?)", "Program.<signature>(int)").WithLocation(9, 13));
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(typeName);
        }
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
        string expectedIL = (interfaceType == "IReadOnlyDictionary") ?
            """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor()"
                  IL_0005:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_000a:  ret
                }
                """ :
            """
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
        string sourceA = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Pair(null, 1, "one").Report();
                    }
                    static {{interfaceType}}<K, V> Pair<K, V>(IEqualityComparer<K> c, K k, V v)
                    {
                        return [with(c), k:v];
                    }
                }
                """;
        var comp = CreateCompilation([sourceA, s_collectionExtensions], options: TestOptions.ReleaseExe);
        comp.VerifyDiagnostics();
        var verifier = CompileAndVerify(
            comp,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[[1, one]], "));
        verifier.VerifyIL("Program.Pair<K, V>", (interfaceType == "IReadOnlyDictionary") ?
            """
                {
                  // Code size       20 (0x14)
                  .maxstack  4
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0006:  dup
                  IL_0007:  ldarg.1
                  IL_0008:  ldarg.2
                  IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000e:  newobj     "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>..ctor(System.Collections.Generic.IDictionary<K, V>)"
                  IL_0013:  ret
                }
                """ :
            """
                {
                  // Code size       15 (0xf)
                  .maxstack  4
                  IL_0000:  ldarg.0
                  IL_0001:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0006:  dup
                  IL_0007:  ldarg.1
                  IL_0008:  ldarg.2
                  IL_0009:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_000e:  ret
                }
                """);

        string sourceB = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Expression(null, new KeyValuePair<int, string>(2, "two")).Report();
                    }
                    static {{interfaceType}}<K, V> Expression<K, V>(IEqualityComparer<K> c, KeyValuePair<K, V> e)
                    {
                        return [with(1, c), e];
                    }
                }
                """;
        comp = CreateCompilation([sourceB, s_collectionExtensions], options: TestOptions.ReleaseExe);
        if (interfaceType == "IReadOnlyDictionary")
        {
            comp.VerifyDiagnostics(
                // (10,16): error CS1501: No overload for method '<signature>' takes 2 arguments
                //         return [with(1, c), e];
                Diagnostic(ErrorCode.ERR_BadArgCount, "[with(1, c), e]").WithArguments("<signature>", "2").WithLocation(10, 16));
        }
        else
        {
            comp.VerifyDiagnostics();
            verifier = CompileAndVerify(
                comp,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("[[2, two]], "));
            verifier.VerifyIL("Program.Expression<K, V>", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  4
                  .locals init (System.Collections.Generic.KeyValuePair<K, V> V_0)
                  IL_0000:  ldc.i4.1
                  IL_0001:  ldarg.0
                  IL_0002:  newobj     "System.Collections.Generic.Dictionary<K, V>..ctor(int, System.Collections.Generic.IEqualityComparer<K>)"
                  IL_0007:  ldarg.1
                  IL_0008:  stloc.0
                  IL_0009:  dup
                  IL_000a:  ldloca.s   V_0
                  IL_000c:  call       "K System.Collections.Generic.KeyValuePair<K, V>.Key.get"
                  IL_0011:  ldloca.s   V_0
                  IL_0013:  call       "V System.Collections.Generic.KeyValuePair<K, V>.Value.get"
                  IL_0018:  callvirt   "void System.Collections.Generic.Dictionary<K, V>.this[K].set"
                  IL_001d:  ret
                }
                """);
        }

        string sourceC = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static {{interfaceType}}<K, V> Pair<K, V>(IEqualityComparer<K> c, K k, V v)
                    {
                        return [k:v, with(c)];
                    }
                }
                """;
        comp = CreateCompilation(sourceC);
        comp.VerifyEmitDiagnostics(
            // (6,22): error CS9501: Collection argument element must be the first element.
            //         return [k:v, with(1)];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(6, 22));
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_MissingMember_01(
        [CombinatorialValues(
                "IEnumerable",
                "IReadOnlyCollection",
                "IReadOnlyList",
                "ICollection",
                "IList")]
            string typeName,
        [CombinatorialValues(
                0,
                WellKnownMember.System_Collections_Generic_List_T__ctor,
                WellKnownMember.System_Collections_Generic_List_T__ctorInt32,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_IEqualityComparer_K,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32_IEqualityComparer_K)]
            int missingMember)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Create(int i)
                    {
                        {{typeName}}<int> c;
                        c = [];
                        c = [with()];
                        c = [with(i)];
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.MakeMemberMissing((WellKnownMember)missingMember);
        if (typeName is "ICollection" or "IList")
        {
            switch ((WellKnownMember)missingMember)
            {
                case WellKnownMember.System_Collections_Generic_List_T__ctor:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(7, 13),
                        // (7,13): error CS7036: There is no argument given that corresponds to the required parameter 'capacity' of 'Program.<signature>(int)'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[]").WithArguments("capacity", "Program.<signature>(int)").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(8, 13),
                        // (8,13): error CS7036: There is no argument given that corresponds to the required parameter 'capacity' of 'Program.<signature>(int)'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[with()]").WithArguments("capacity", "Program.<signature>(int)").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(9, 13));
                    break;
                case WellKnownMember.System_Collections_Generic_List_T__ctorInt32:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(9, 13),
                        // (9,13): error CS1501: No overload for method '<signature>' takes 1 arguments
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i)]").WithArguments("<signature>", "1").WithLocation(9, 13));
                    break;
                default:
                    comp.VerifyEmitDiagnostics();
                    break;
            }
        }
        else
        {
            switch ((WellKnownMember)missingMember)
            {
                case WellKnownMember.System_Collections_Generic_List_T__ctor:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.List`1..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.List`1", ".ctor").WithLocation(9, 13));
                    break;
                default:
                    comp.VerifyEmitDiagnostics(
                        // (9,13): error CS1501: No overload for method '<signature>' takes 1 arguments
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i)]").WithArguments("<signature>", "1").WithLocation(9, 13));
                    break;
            }
        }
    }

    [Theory]
    [CombinatorialData]
    public void InterfaceTarget_MissingMember_02(
        [CombinatorialValues(
                "IDictionary",
                "IReadOnlyDictionary")]
            string typeName,
        [CombinatorialValues(
                0,
                WellKnownMember.System_Collections_Generic_List_T__ctor,
                WellKnownMember.System_Collections_Generic_List_T__ctorInt32,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_IEqualityComparer_K,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32,
                WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32_IEqualityComparer_K)]
            int missingMember)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Create(int i, IEqualityComparer<int> e)
                    {
                        {{typeName}}<int, string> c;
                        c = [];
                        c = [with()];
                        c = [with(i)];
                        c = [with(e)];
                        c = [with(i, e)];
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.MakeMemberMissing((WellKnownMember)missingMember);
        if (typeName is "IDictionary")
        {
            switch ((WellKnownMember)missingMember)
            {
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (7,13): error CS1501: No overload for method '<signature>' takes 0 arguments
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[]").WithArguments("<signature>", "0").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (8,13): error CS1501: No overload for method '<signature>' takes 0 arguments
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with()]").WithArguments("<signature>", "0").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13));
                    break;
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_IEqualityComparer_K:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (10,19): error CS1503: Argument 1: cannot convert from 'System.Collections.Generic.IEqualityComparer<int>' to 'int'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_BadArgType, "e").WithArguments("1", "System.Collections.Generic.IEqualityComparer<int>", "int").WithLocation(10, 19),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13));
                    break;
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (9,19): error CS1503: Argument 1: cannot convert from 'int' to 'System.Collections.Generic.IEqualityComparer<int>?'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgType, "i").WithArguments("1", "int", "System.Collections.Generic.IEqualityComparer<int>?").WithLocation(9, 19),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13));
                    break;
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_Int32_IEqualityComparer_K:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13),
                        // (11,13): error CS1501: No overload for method '<signature>' takes 2 arguments
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i, e)]").WithArguments("<signature>", "2").WithLocation(11, 13));
                    break;
                default:
                    comp.VerifyEmitDiagnostics();
                    break;
            }
        }
        else
        {
            switch ((WellKnownMember)missingMember)
            {
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (7,13): error CS7036: There is no argument given that corresponds to the required parameter 'comparer' of 'Program.<signature>(IEqualityComparer<int>?)'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[]").WithArguments("comparer", "Program.<signature>(System.Collections.Generic.IEqualityComparer<int>?)").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (8,13): error CS7036: There is no argument given that corresponds to the required parameter 'comparer' of 'Program.<signature>(IEqualityComparer<int>?)'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "[with()]").WithArguments("comparer", "Program.<signature>(System.Collections.Generic.IEqualityComparer<int>?)").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (9,19): error CS1503: Argument 1: cannot convert from 'int' to 'System.Collections.Generic.IEqualityComparer<int>?'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgType, "i").WithArguments("1", "int", "System.Collections.Generic.IEqualityComparer<int>?").WithLocation(9, 19),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13),
                        // (11,13): error CS1501: No overload for method '<signature>' takes 2 arguments
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i, e)]").WithArguments("<signature>", "2").WithLocation(11, 13));
                    break;
                case WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor_IEqualityComparer_K:
                    comp.VerifyEmitDiagnostics(
                        // (7,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(7, 13),
                        // (8,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with()];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with()]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(8, 13),
                        // (9,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(9, 13),
                        // (9,13): error CS1501: No overload for method '<signature>' takes 1 arguments
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i)]").WithArguments("<signature>", "1").WithLocation(9, 13),
                        // (10,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(10, 13),
                        // (10,13): error CS1501: No overload for method '<signature>' takes 1 arguments
                        //         c = [with(e)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(e)]").WithArguments("<signature>", "1").WithLocation(10, 13),
                        // (11,13): error CS0656: Missing compiler required member 'System.Collections.Generic.Dictionary`2..ctor'
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "[with(i, e)]").WithArguments("System.Collections.Generic.Dictionary`2", ".ctor").WithLocation(11, 13),
                        // (11,13): error CS1501: No overload for method '<signature>' takes 2 arguments
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i, e)]").WithArguments("<signature>", "2").WithLocation(11, 13));
                    break;
                default:
                    comp.VerifyEmitDiagnostics(
                        // (9,19): error CS1503: Argument 1: cannot convert from 'int' to 'System.Collections.Generic.IEqualityComparer<int>?'
                        //         c = [with(i)];
                        Diagnostic(ErrorCode.ERR_BadArgType, "i").WithArguments("1", "int", "System.Collections.Generic.IEqualityComparer<int>?").WithLocation(9, 19),
                        // (11,13): error CS1501: No overload for method '<signature>' takes 2 arguments
                        //         c = [with(i, e)];
                        Diagnostic(ErrorCode.ERR_BadArgCount, "[with(i, e)]").WithArguments("<signature>", "2").WithLocation(11, 13));
                    break;
            }
        }
    }
#endif

    [Theory]
    [CombinatorialData]
    public void List_KnownLength_List(
        [CombinatorialValues("", "with(), ", "with(3), ")] string argsPrefix)
    {
        string source = $$"""
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        Create(1, 2, 3).Report();
                    }
                    static List<T> Create<T>(params T[] items)
                    {
                        return [{{argsPrefix}} ..items];
                    }
                }
                """;
        var verifier = CompileAndVerify(
            [source, s_collectionExtensions],
            targetFramework: TargetFramework.Net80,
            verify: Verification.Skipped,
            expectedOutput: IncludeExpectedOutput("[1, 2, 3], "));
        verifier.VerifyDiagnostics();
        string expectedIL;
        switch (argsPrefix)
        {
            case "with(), ":
                expectedIL = """
                    {
                      // Code size       15 (0xf)
                      .maxstack  3
                      .locals init (T[] V_0)
                      IL_0000:  ldarg.0
                      IL_0001:  stloc.0
                      IL_0002:  newobj     "System.Collections.Generic.List<T>..ctor()"
                      IL_0007:  dup
                      IL_0008:  ldloc.0
                      IL_0009:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
                      IL_000e:  ret
                    }
                    """;
                break;
            case "with(3), ":
                expectedIL = """
                    {
                        // Code size       16 (0x10)
                        .maxstack  3
                        .locals init (T[] V_0)
                        IL_0000:  ldarg.0
                        IL_0001:  stloc.0
                        IL_0002:  ldc.i4.3
                        IL_0003:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                        IL_0008:  dup
                        IL_0009:  ldloc.0
                        IL_000a:  callvirt   "void System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>)"
                        IL_000f:  ret
                    }
                    """;
                break;
            default:
                expectedIL = """
                        {
                          // Code size        7 (0x7)
                          .maxstack  1
                          IL_0000:  ldarg.0
                          IL_0001:  call       "System.Collections.Generic.List<T> System.Linq.Enumerable.ToList<T>(System.Collections.Generic.IEnumerable<T>)"
                          IL_0006:  ret
                        }
                        """;
                break;
        }
        verifier.VerifyIL("Program.Create<T>", expectedIL);
    }

    [Fact]
    public void DefiniteAssignment_01()
    {
        string source = """
                using System.Collections.Generic;
                class Program
                {
                    static HashSet<T> Create<T>()
                    {
                        IEqualityComparer<T> e = null;
                        return [with(e)];
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void DefiniteAssignment_02()
    {
        string source = """
                using System.Collections.Generic;
                class Program
                {
                    static IEqualityComparer<T> Create<T>()
                    {
                        IEqualityComparer<T> e;
                        HashSet<T> s = [with(e = null)];
                        return e;
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void NullableAnalysis_01()
    {
        string source = """
                #nullable enable
                using System.Collections.Generic;
                class Program
                {
                    static IEqualityComparer<T> Create<T>()
                    {
                        IEqualityComparer<T>? e = null;
                        HashSet<T> s = [with(e = Create<T>())];
                        return e;
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
    }

    [Fact]
    public void NullableAnalysis_02()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            class Program
            {
                static IEqualityComparer<T> Create<T>()
                {
                    IEqualityComparer<T>? e = null;
                    HashSet<string> s = ["", with(e = Create<T>())];
                    return e;
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,34): error CS9400: 'with(...)' element must be the first element
            //         HashSet<string> s = ["", with(e = Create<T>())];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 34));
    }

    [Fact]
    public void NullableAnalysis_03()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            class Program
            {
                static IEqualityComparer<T> Create<T>()
                {
                    IEqualityComparer<T>? e = null;
                    HashSet<string> s = [e.ToString(), with(e = Create<T>())];
                    return e;
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,30): warning CS8602: Dereference of a possibly null reference.
            //         HashSet<string> s = [e.ToString(), with(e = Create<T>())];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "e").WithLocation(8, 30),
            // (8,44): error CS9400: 'with(...)' element must be the first element
            //         HashSet<string> s = [e.ToString(), with(e = Create<T>())];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 44));
    }

    [Fact]
    public void NullableAnalysis_04()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            class Program
            {
                static IEqualityComparer<T> Create<T>()
                {
                    IEqualityComparer<T>? e = Create<T>();
                    HashSet<string> s = [(e = null).ToString(), with((IEqualityComparer<string>)(object)e.ToString())];
                    return e;
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,31): warning CS8602: Dereference of a possibly null reference.
            //         HashSet<string> s = [(e = null).ToString(), with((IEqualityComparer<string>)(object)e.ToString())];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "e = null").WithLocation(8, 31),
            // (8,53): error CS9400: 'with(...)' element must be the first element
            //         HashSet<string> s = [(e = null).ToString(), with((IEqualityComparer<string>)(object)e.ToString())];
            Diagnostic(ErrorCode.ERR_CollectionArgumentsMustBeFirst, "with").WithLocation(8, 53));
    }

    [Fact]
    public void NullableAnalysis_05()
    {
        string source = """
                #nullable enable
                using System.Collections.Generic;
                class Program
                {
                    static IEqualityComparer<string> Create()
                    {
                        IEqualityComparer<string>? e = Create();
                        HashSet<string> s = [with((e = null)), e.ToString()];
                        return e;
                    }
                }
                """;
        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (8,48): warning CS8602: Dereference of a possibly null reference.
            //         HashSet<string> s = [with((e = null)), e.ToString()];
            Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "e").WithLocation(8, 48));
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
            // (3,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
            // c = [with()];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with()").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(3, 6),
            // (5,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(5, 6));
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
            // (5,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection()'.
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection()").WithLocation(5, 6));
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
            // (3,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
            // c = [with()];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with()").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(3, 6),
            // (5,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(params MyCollection<T>)'.
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(params MyCollection<T>)").WithLocation(5, 6),
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
            // (3,6): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'MyCollection<int>.MyCollection(int, params MyCollection<int>)'
            // c = [with()];
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "with()").WithArguments("x", "MyCollection<int>.MyCollection(int, params MyCollection<int>)").WithLocation(3, 6),
            // (4,6): error CS9223: Creation of params collection 'MyCollection<int>' results in an infinite chain of invocation of constructor 'MyCollection<T>.MyCollection(T, params MyCollection<T>)'.
            // c = [with(1)];
            Diagnostic(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, "with(1)").WithArguments("MyCollection<int>", "MyCollection<T>.MyCollection(T, params MyCollection<T>)").WithLocation(4, 6),
            // (8,30): error CS9228: Non-array params collection type must have an applicable constructor that can be called with no arguments.
            //     public MyCollection(T x, params MyCollection<T> y)
            Diagnostic(ErrorCode.ERR_ParamsCollectionMissingConstructor, "params MyCollection<T> y").WithLocation(8, 30));
    }

    [Fact]
    public void CollectionBuilderOverloadResolutionPriority()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string s, object o, ReadOnlySpan<T> items)
                {
                    Console.WriteLine("Called first overload");
                    return new(items);
                }

                [OverloadResolutionPriority(1)]
                public static MyCollection<T> Create<T>(object o, string s, ReadOnlySpan<T> items)
                {
                    Console.WriteLine("Called second overload");
                    return new(items);
                }
            }
            """;
        string sourceB = """
            using System;
            class Program
            {
                static void Main()
                {
                    MyCollection<string> c = [with("", ""), ""];
                }
            }
            """;
        var comp = CompileAndVerify(
            [sourceA, sourceB, OverloadResolutionPriorityAttributeDefinition],
            targetFramework: TargetFramework.Net80,
            expectedOutput: IncludeExpectedOutput(
                """
                Called second overload
                """), verify: Verification.FailsPEVerify).VerifyIL("Program.Main", """
                {
                  // Code size       30 (0x1e)
                  .maxstack  3
                  .locals init (string V_0)
                  IL_0000:  ldstr      ""
                  IL_0005:  ldstr      ""
                  IL_000a:  ldstr      ""
                  IL_000f:  stloc.0
                  IL_0010:  ldloca.s   V_0
                  IL_0012:  newobj     "System.ReadOnlySpan<string>..ctor(ref readonly string)"
                  IL_0017:  call       "MyCollection<string> MyBuilder.Create<string>(object, string, System.ReadOnlySpan<string>)"
                  IL_001c:  pop
                  IL_001d:  ret
                }
                """);
    }

    [Fact]
    public void CollectionBuilderNoOverloadResolutionPriority()
    {
        string sourceA = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public MyCollection(ReadOnlySpan<T> items) {
                }
                IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            class MyBuilder
            {
                public static MyCollection<T> Create<T>(string s, object o, ReadOnlySpan<T> items)
                {
                    return new(items);
                }

                public static MyCollection<T> Create<T>(object o, string s, ReadOnlySpan<T> items)
                {
                    return new(items);
                }
            }
            """;
        string sourceB = """
            class Program
            {
                static void Main()
                {
                    MyCollection<string> c = [with("", ""), ""];
                }
            }
            """;
        var comp = CreateCompilation(
            [sourceA, sourceB],
            targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (5,35): error CS0121: The call is ambiguous between the following methods or properties: 'MyBuilder.Create<string>(string, object, ReadOnlySpan<string>)' and 'MyBuilder.Create<string>(object, string, ReadOnlySpan<string>)'
                //         MyCollection<string> c = [with("", ""), ""];
                Diagnostic(ErrorCode.ERR_AmbigCall, @"with("""", """")").WithArguments("MyBuilder.Create<string>(string, object, System.ReadOnlySpan<string>)", "MyBuilder.Create<string>(object, string, System.ReadOnlySpan<string>)"));
    }

    [Fact]
    public void InterpolatedStringHandler()
    {
        var code = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Collections.Generic;

            public class C : List<int>
            {
                public C(int i, string s, [InterpolatedStringHandlerArgumentAttribute("i", "s")] CustomHandler c) => Console.WriteLine(c.ToString());
            }

            public partial struct CustomHandler
            {
                public CustomHandler(int literalLength, int formattedCount, int i, string s) : this(literalLength, formattedCount)
                {
                    _builder.AppendLine("i:" + i.ToString());
                    _builder.AppendLine("s:" + s);
                }
            }
            """;

        var executableCode = """
            class Program
            {
                static void Main()
                {
                    int i = 10;
                    string s = "arg";
                    C c = [with(i, s, $"" + $"literal")];
                }
            }
            """;

        var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

        CompileAndVerify([code, executableCode, InterpolatedStringHandlerArgumentAttribute, handler], expectedOutput: """
                i:10
                s:arg
                literal:literal
                """)
            .VerifyDiagnostics()
            .VerifyIL("Program.Main", """
                {
                  // Code size       45 (0x2d)
                  .maxstack  7
                  .locals init (string V_0, //s
                                int V_1,
                                string V_2,
                                CustomHandler V_3)
                  IL_0000:  ldc.i4.s   10
                  IL_0002:  ldstr      "arg"
                  IL_0007:  stloc.0
                  IL_0008:  stloc.1
                  IL_0009:  ldloc.1
                  IL_000a:  ldloc.0
                  IL_000b:  stloc.2
                  IL_000c:  ldloc.2
                  IL_000d:  ldloca.s   V_3
                  IL_000f:  ldc.i4.7
                  IL_0010:  ldc.i4.0
                  IL_0011:  ldloc.1
                  IL_0012:  ldloc.2
                  IL_0013:  call       "CustomHandler..ctor(int, int, int, string)"
                  IL_0018:  ldloca.s   V_3
                  IL_001a:  ldstr      "literal"
                  IL_001f:  call       "bool CustomHandler.AppendLiteral(string)"
                  IL_0024:  pop
                  IL_0025:  ldloc.3
                  IL_0026:  newobj     "C..ctor(int, string, CustomHandler)"
                  IL_002b:  pop
                  IL_002c:  ret
                }
                """);
    }

    [Fact]
    public void WithOutsideCollectionIsAnInvocation()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    N(with(capacity: 0), 1, 2, 3);
                }

                void N(params List<int> list) { }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (6,11): error CS0103: The name 'with' does not exist in the current context
            //         N(with(capacity: 0), 1, 2, 3);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "with").WithArguments("with").WithLocation(6, 11));
    }
}
