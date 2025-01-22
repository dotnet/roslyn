// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionExpressionArgumentTests : CSharpTestBase
    {
        private const string s_collectionExtensions = CollectionExpressionTests.s_collectionExtensions;

        // PROTOTYPE: Test [with()] and [with(arg)] with each of the target type kinds.
        // PROTOTYPE: Test [x, with()].
        // PROTOTYPE: Test [with(args1), with(args2)] where one or both of args1 and args2 may be empty.
        // PROTOTYPE: Test [with()] with generic type parameter target type.
        // PROTOTYPE: Test .ctor or factory method with generic constraints that are/are not satisfied by arguments.
        // PROTOTYPE: Test binding within later args: eg. [with(), with(too, many, args)].
        // PROTOTYPE: Test unrecognized parameter name.
        // PROTOTYPE: Test reordered parameters.
        // PROTOTYPE: Test order of evaluation, including with reordered parameters.
        // PROTOTYPE: Test params.
        // PROTOTYPE: Test dynamic arguments.
        // PROTOTYPE: Test with(arg) for collection initializer target type that does not have a parameterless constructor.
        // PROTOTYPE: Test no args and empty with() for collection builder type with:
        // - no factory method that takes no arguments
        // - factory method that has optional parameters
        // - factory method that has params parameter

        public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void LanguageVersion_01(LanguageVersion languageVersion)
        {
            string source = """
                using System.Collections.Generic;
                List<int> l;
                l = [with()];
                l = [with(capacity: 3), 1, 2, 3];
                l = [4, with()];
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            // PROTOTYPE: Should report error for with() following an element
            // in [4, with()], and regardless of language version.
            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,6): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // l = [with()];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(3, 6),
                    // (4,6): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // l = [with(capacity: 3), 1, 2, 3];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(4, 6),
                    // (5,9): error CS8652: The feature 'collection expression arguments' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // l = [4, with()];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("collection expression arguments").WithLocation(5, 9));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        // PROTOTYPE: Test with each of the target type kinds.
        // PROTOTYPE: Test with generic parameter U where U : new(), IEnumerable<T>, and where T : struct, IEnumerable<T>.
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
                targetFramework: TargetFramework.Net90,
                expectedOutput: "[], [], ");
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
                targetFramework: TargetFramework.Net90,
                expectedOutput: "[1, 2], [3, 4], ");
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

        // PROTOTYPE: Test with each of the target type kinds.
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
                  IL_0000:  ldc.i4.1
                  IL_0001:  newobj     "System.Collections.Generic.List<T>..ctor(int)"
                  IL_0006:  dup
                  IL_0007:  ldarg.0
                  IL_0008:  callvirt   "void System.Collections.Generic.List<T>.Add(T)"
                  IL_000d:  ret
                }
                """);
        }
    }
}
