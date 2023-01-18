// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralTests : CSharpTestBase
    {
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
                        List<object> l;
                        l = [];
                        l = [1, 2, 3];
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion == LanguageVersion.CSharp11)
            {
                comp.VerifyEmitDiagnostics(
                    // (7,13): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         l = [];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(7, 13),
                    // (8,13): error CS8652: The feature 'collection literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         l = [1, 2, 3];
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "[").WithArguments("collection literals").WithLocation(8, 13));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        // PROTOTYPE: Test with different collection types: class, struct, array, string, etc.
        // PROTOTYPE: Test with types that are not constructible: non-collection type, static type, interface, abstract type, type parameter, etc.
        // PROTOTYPE: Test with type parameter T where T : new(), IEnumerable<U>, and with struct, class, or neither constraint.
        // PROTOTYPE: Test with explicit cast rather than target type, with collection type, or base type, etc.
        // PROTOTYPE: Test with Nullable<T> where T is a collection type. See also LocalRewriter.VisitConversion() which has special handling for ConversionKind.ObjectCreation with Nullable<T>.
        // PROTOTYPE: Test with missing constructor; inaccessible constructor; implicit (default) constructor; constructor with unexpected parameters; constructor with optional parameters; constructor with params parameter.
        [Fact]
        public void CollectionInitializerType_Empty()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<long?> c = Create();
                        Console.WriteLine(c.Count);
                    }
                    static List<long?> Create() => [];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "0");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  ret
                }
                """);
        }

        [Fact]
        public void CollectionInitializerType_WithElements()
        {
            string source = """
                using System;
                using System.Collections.Generic;
                class Program
                {
                    static void Main()
                    {
                        List<long?> c = Create();
                        Console.WriteLine((c.Count, c[0], c[1] is null));
                    }
                    static List<long?> Create() => [1, null];
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(2, 1, True)");
            verifier.VerifyIL("Program.Create", """
                {
                  // Code size       34 (0x22)
                  .maxstack  3
                  .locals init (long? V_0)
                  IL_0000:  newobj     "System.Collections.Generic.List<long?>..ctor()"
                  IL_0005:  dup
                  IL_0006:  ldc.i4.1
                  IL_0007:  conv.i8
                  IL_0008:  newobj     "long?..ctor(long)"
                  IL_000d:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0012:  dup
                  IL_0013:  ldloca.s   V_0
                  IL_0015:  initobj    "long?"
                  IL_001b:  ldloc.0
                  IL_001c:  callvirt   "void System.Collections.Generic.List<long?>.Add(long?)"
                  IL_0021:  ret
                }
                """);
        }

        [Fact]
        public void NotConstructibleType_Empty()
        {
            string source = """
                C c = [];
                class C { }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,7): error CS9105: Cannot initialize type 'C' with a collection literal because the type is not constructible.
                // C c = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("C").WithLocation(1, 7));
        }

        [Fact]
        public void CollectionInitializerType_Struct()
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
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
                    static void M1()
                    {
                        S1<int> s;
                        s = [];
                        s = [1, 2];
                    }
                    static void M2()
                    {
                        S2<int> s;
                        s = [];
                        s = [1, 2];
                    }
                }
                """;
            var verifier = CompileAndVerify(source);
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

        [ConditionalFact(typeof(ClrOnly))]
        public void ImmutableArray_Empty()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArray<long?> c = Create();
                        Console.WriteLine((c.Length, c.IsEmpty));
                    }
                    static ImmutableArray<long?> Create() => [];
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, expectedOutput: "(0, True)");
            verifier.VerifyIL("Program.Create", """
                {
                    ...
                }
                """);
        }

        [ConditionalFact(typeof(ClrOnly))]
        public void ImmutableArray_WithElements()
        {
            string source = """
                using System;
                using System.Collections.Immutable;
                class Program
                {
                    static void Main()
                    {
                        ImmutableArray<long?> c = Create();
                        Console.WriteLine((c.Length, c[0], c[1] is null));
                    }
                    static ImmutableArray<long?> Create() => [1, null];
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net70, expectedOutput: "(2, 1, True)");
            verifier.VerifyIL("Program.Create", """
                {
                    ...
                }
                """);
        }
    }
}
