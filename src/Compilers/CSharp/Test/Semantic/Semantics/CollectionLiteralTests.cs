// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralTests : CSharpTestBase
    {
        private static string GetCollectionExtensions(bool includeSpan = false)
        {
            string source = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Text;
                static partial class CollectionExtensions
                {
                    private static StringBuilder Begin()
                    {
                        var builder = new StringBuilder();
                        builder.Append("[");
                        return builder;
                    }
                    private static string End(StringBuilder builder)
                    {
                        builder.Append("]");
                        return builder.ToString();
                    }
                    private static void Append(StringBuilder builder, object value)
                    {
                        if (builder.Length > 1) builder.Append(", ");
                        builder.Append(value is null ? "null" : value.ToString());
                    }
                    internal static void Report<T>(this IEnumerable<T> e)
                    {
                        var b = Begin();
                        foreach (var i in e) Append(b, i);
                        Console.Write(End(b));
                        Console.Write(", ");
                    }
                    internal static void Report(this object o)
                    {
                        var b = Begin();
                        foreach (var i in ((IEnumerable)o)) Append(b, i);
                        Console.Write(End(b));
                        Console.Write(", ");
                    }
                }
                """;
            if (includeSpan)
            {
                source += """
                    static partial class CollectionExtensions
                    {
                        internal static void Report<T>(this in Span<T> s)
                        {
                            Report((ReadOnlySpan<T>)s);
                        }
                        internal static void Report<T>(this in ReadOnlySpan<T> s)
                        {
                            var b = Begin();
                            foreach (var i in s) Append(b, i);
                            Console.Write(End(b));
                            Console.Write(", ");
                        }
                    }
                    """;
            }
            return source;
        }

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
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object x = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9500: Cannot initialize type 'dynamic' with a collection literal because the type is not constructible.
                //         dynamic y = [];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,13): error CS0815: Cannot assign collection literals to an implicitly-typed variable
                //         var z = [];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "z = []").WithArguments("collection literals").WithLocation(7, 13));
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
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,20): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object x = [1];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[1]").WithArguments("object").WithLocation(5, 20),
                // (6,21): error CS9500: Cannot initialize type 'dynamic' with a collection literal because the type is not constructible.
                //         dynamic y = [2];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[2]").WithArguments("dynamic").WithLocation(6, 21),
                // (7,13): error CS0815: Cannot assign collection literals to an implicitly-typed variable
                //         var z = [3];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "z = [3]").WithArguments("collection literals").WithLocation(7, 13));
        }

        [Fact]
        public void NaturalType_03()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        object[] x = [[]];
                        object[] y = [[2]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,23): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object[] x = [[]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[]").WithArguments("object").WithLocation(5, 23),
                // (6,23): error CS9500: Cannot initialize type 'object' with a collection literal because the type is not constructible.
                //         object[] y = [[2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[2]").WithArguments("object").WithLocation(6, 23));
        }

        [Fact]
        public void NaturalType_04()
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
        public void NaturalType_05()
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
            // PROTOTYPE: Verify expectedOutput: "..." when natural type is supported.
            var comp = CreateCompilation(new[] { source, GetCollectionExtensions() });
            comp.VerifyEmitDiagnostics(
                // (5,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([1, 2]).Report();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(5, 9),
                // (6,9): error CS0411: The type arguments for method 'Program.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F([[3, 4]]).Report();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(T)").WithLocation(6, 9));
        }

        [Fact]
        public void NaturalType_06()
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
        public void NaturalType_07()
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
            var verifier = CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
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
            CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [null, 2], ");
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
            var verifier = CompileAndVerify(new[] { source, GetCollectionExtensions(includeSpan: true) }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
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
            CompileAndVerify(new[] { source, GetCollectionExtensions(includeSpan: true) }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2, 3], ");
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
            CompileAndVerify(new[] { source, GetCollectionExtensions(includeSpan: true) }, targetFramework: TargetFramework.Net70, verify: Verification.Skipped, expectedOutput: "[], [1, 2, 3], ");
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
            comp.VerifyDiagnostics(
                // (6,20): error CS8347: Cannot use a result of 'Program.F2<int>(in Span<int>)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_EscapeCall, "F2<int>([])").WithArguments($"Program.F2<int>(in System.{spanType}<int>)", "s").WithLocation(6, 20),
                // (6,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref F2<int>([]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "[]").WithLocation(6, 28));
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
            var verifier = CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [1, 2], [3, 4, 5], [null, 7], ");
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
                // (3,5): error CS9500: Cannot initialize type 'S' with a collection literal because the type is not constructible.
                // s = [1, 2];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[1, 2]").WithArguments("S").WithLocation(3, 5));
        }

        [Fact]
        public void CollectionInitializerType_03()
        {
            string source = """
                C c;
                c = [];
                c = [1, 2];
                class C
                {
                    C(object o) { }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[]").WithArguments("C", "0").WithLocation(2, 5),
                // (3,5): error CS1729: 'C' does not contain a constructor that takes 0 arguments
                // c = [1, 2];
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "[1, 2]").WithArguments("C", "0").WithLocation(3, 5));
        }

        [Fact]
        public void CollectionInitializerType_04()
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
        public void CollectionInitializerType_05()
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
            CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [], [1, 2], [3, 4], ");
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
            CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [], [1, 2], [3, 4], ");
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
            CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [], [1, 2], [3, 4], ");
        }

        // PROTOTYPE: Test constructor use-site error.

        [Fact]
        public void CollectionInitializerType_06()
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
        public void CollectionInitializerType_07()
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
        public void CollectionInitializerType_08()
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
        public void CollectionInitializerType_09()
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
        public void CollectionInitializerType_10()
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

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public void CollectionInitializerType_TypeParameter_01(string type)
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
            var verifier = CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[], [null, 2], ");
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
        public void CollectionInitializerType_TypeParameter_02()
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

        // PROTOTYPE: Test variations of Add() methods - inaccessible, conversion errors, use-site errors, optional parameters, params array, etc.

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

        [Fact]
        public void SpreadElement_01()
        {
            string source = """
                class Program
                {
                    static void Main()
                    {
                        int[] a;
                        a = [];
                        a = [..a, ..[1, 2]];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,14): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         a = [..a, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..a").WithLocation(7, 14),
                // (7,19): error CS9502: Support for collection literal dictionary and spread elements has not been implemented.
                //         a = [..a, ..[1, 2]];
                Diagnostic(ErrorCode.ERR_CollectionLiteralElementNotImplemented, "..[1, 2]").WithLocation(7, 19));
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
            // PROTOTYPE: Should // 2 be reported as an error? Do we report this for the array initializer case?
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
                // (15,24): error CS9500: Cannot initialize type 'S<object>?' with a collection literal because the type is not constructible.
                //         S<object>? y = [1];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[1]").WithArguments("S<object>?").WithLocation(15, 24),
                // (16,13): error CS9500: Cannot initialize type 'S<object>?' with a collection literal because the type is not constructible.
                //         y = [2];
                Diagnostic(ErrorCode.ERR_CollectionLiteralTargetTypeNotConstructible, "[2]").WithArguments("S<object>?").WithLocation(16, 13));
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

        [ConditionalFact(typeof(DesktopOnly))]
        public void RestrictedTypes_01()
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
                // (6,13): error CS0815: Cannot assign collection literals to an implicitly-typed variable
                //         var x = [default(TypedReference)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = [default(TypedReference)]").WithArguments("collection literals").WithLocation(6, 13),
                // (7,13): error CS0815: Cannot assign collection literals to an implicitly-typed variable
                //         var y = [default(ArgIterator)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "y = [default(ArgIterator)]").WithArguments("collection literals").WithLocation(7, 13),
                // (8,13): error CS0815: Cannot assign collection literals to an implicitly-typed variable
                //         var z = [default(RuntimeArgumentHandle)];
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "z = [default(RuntimeArgumentHandle)]").WithArguments("collection literals").WithLocation(8, 13));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void RestrictedTypes_02()
        {
            string source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        UnknownType u;
                        u = [default(TypedReference)];
                        u = [default(ArgIterator)];
                        u = [default(RuntimeArgumentHandle)];
                    }
                }
                """;
            var comp = CreateCompilation(source);
            // PROTOTYPE: Should report errors for restricted types.
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'UnknownType' could not be found (are you missing a using directive or an assembly reference?)
                //         UnknownType u;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UnknownType").WithArguments("UnknownType").WithLocation(6, 9));
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
        public void IOperation_TypeParameter()
        {
            string source = """
                using System;
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
            // PROTOTYPE: Where is the ICollectionLiteralOperation node?
            string expectedOperationTree =
@"ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsImplicit) (Syntax: '[a, b]')
    Initializer:
      null
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
";
            VerifyOperationTreeForTest<Syntax.CollectionCreationExpressionSyntax>(source, expectedOperationTree);
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
            var verifier = CompileAndVerify(new[] { source, GetCollectionExtensions() }, expectedOutput: "[1, 2], [3, 4], ");
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
