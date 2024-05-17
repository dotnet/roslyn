// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class FirstClassSpanTests : CSharpTestBase
{
    public static TheoryData<LanguageVersion> LangVersions()
    {
        return new TheoryData<LanguageVersion>()
        {
            LanguageVersion.CSharp12,
            LanguageVersionFacts.CSharpNext,
            LanguageVersion.Preview,
        };
    }

    private sealed class CombinatorialLangVersions()
        : CombinatorialValuesAttribute(LangVersions().Select(d => d.Single()).ToArray());

    [Fact, WorkItem("https://github.com/dotnet/runtime/issues/101261")]
    public void Example_StringValuesAmbiguity()
    {
        var source = """
            using System;

            Console.Write(C.M(new StringValues()));

            static class C
            {
                public static string M(StringValues sv) => StringExtensions.Join(",", sv);
            }

            static class StringExtensions
            {
                public static string Join(string separator, params string[] values) => "array";
                public static string Join(string separator, params ReadOnlySpan<string> values) => "span";
            }

            readonly struct StringValues
            {
                public static implicit operator string(StringValues values) => null;
                public static implicit operator string[](StringValues value) => null;
            }
            """;

        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (7,65): error CS0121: The call is ambiguous between the following methods or properties: 'StringExtensions.Join(string, params string[])' and 'StringExtensions.Join(string, params ReadOnlySpan<string>)'
            //     public static string M(StringValues sv) => StringExtensions.Join(",", sv);
            Diagnostic(ErrorCode.ERR_AmbigCall, "Join").WithArguments("StringExtensions.Join(string, params string[])", "StringExtensions.Join(string, params System.ReadOnlySpan<string>)").WithLocation(7, 65),
            // (13,49): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static string Join(string separator, params ReadOnlySpan<string> values) => "span";
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "params ReadOnlySpan<string> values").WithArguments("params collections").WithLocation(13, 49));

        var expectedOutput = "array";

        var expectedIl = """
            {
              // Code size       17 (0x11)
              .maxstack  2
              IL_0000:  ldstr      ","
              IL_0005:  ldarg.0
              IL_0006:  call       "string[] StringValues.op_Implicit(StringValues)"
              IL_000b:  call       "string StringExtensions.Join(string, params string[])"
              IL_0010:  ret
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void BreakingChange_Inheritance_UserDefinedConversion_ArrayToSpan()
    {
        var source = """
            using System;

            var a = new string[0];
            var d = new Derived();
            d.M(a);

            class Base
            {
                public void M(Span<string> s) => Console.Write("Base");
            }

            class Derived : Base
            {
                public static implicit operator Derived(Span<string> r) => new Derived();

                public void M(Derived s) => Console.WriteLine("Derived");
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        CompileAndVerify(comp, expectedOutput: "Base").VerifyDiagnostics();

        var expectedOutput = "Derived";

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpan(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void BreakingChange_ExtensionMethodLookup_ArrayToSpan()
    {
        var source = """
            using System;

            namespace N1
            {
                using N2;

                public class C
                {
                    public static void Main()
                    {
                        var a = new string[0];
                        a.Test();
                    }
                }

                public static class N1Ext
                {
                    public static void Test(this Span<string> x) => Console.WriteLine("N1");
                }
            }

            namespace N2
            {
                public static class N2Ext
                {
                    public static void Test(this string[] x) => Console.WriteLine("N2");
                }
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "N2").VerifyDiagnostics();

        var expectedOutput = "N1";

        var expectedDiagnostics = new[]
        {
            // (5,5): hidden CS8019: Unnecessary using directive.
            //     using N2;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(5, 5)
        };

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        comp = CreateCompilationWithSpan(source, TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit(
        [CombinatorialValues("Span", "ReadOnlySpan")] string destination,
        bool cast)
    {
        var source = $$"""
            using System;
            {{destination}}<int> s = {{(cast ? $"({destination}<int>)" : "")}}arr();
            report(s);
            static int[] arr() => new int[] { 1, 2, 3 };
            static void report({{destination}}<int> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var expectedOutput = "123";

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", $$"""
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  call       "System.{{destination}}<int> System.{{destination}}<int>.op_Implicit(int[])"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.{{destination}}<int>)"
              IL_000f:  ret
            }
            """);

        var expectedIl = $$"""
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  newobj     "System.{{destination}}<int>..ctor(int[])"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.{{destination}}<int>)"
              IL_000f:  ret
            }
            """;

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MissingCtor()
    {
        var source = """
            using System;
            Span<int> s = arr();
            static int[] arr() => new int[] { 1, 2, 3 };
            """;

        var comp = CreateCompilationWithSpan(source);
        comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
        comp.VerifyDiagnostics(
            // (2,15): error CS0656: Missing compiler required member 'System.Span`1..ctor'
            // Span<int> s = arr();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arr()").WithArguments("System.Span`1", ".ctor").WithLocation(2, 15));
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_SpanTwice()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public Span(T[] array) => Console.Write("{{output}}");
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = """
            using System;
            Span<int> s = arr();
            static int[] arr() => new int[] { 1, 2, 3 };
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, expectedOutput: "Internal");
        verifier.VerifyDiagnostics(
            // (2,1): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
            // Span<int> s = arr();
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Span<int>").WithArguments("", "System.Span<T>", "Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Span<T>").WithLocation(2, 1));

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  newobj     "System.Span<int>..ctor(int[])"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_SemanticModel()
    {
        var source = """
            class C
            {
                System.Span<int> M(int[] arg) { return arg; }
            }
            """;

        var comp = CreateCompilationWithSpan(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var arg = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
        Assert.Equal("arg", arg!.ToString());

        var argType = model.GetTypeInfo(arg);
        Assert.Equal("System.Int32[]", argType.Type.ToTestDisplayString());
        Assert.Equal("System.Span<System.Int32>", argType.ConvertedType.ToTestDisplayString());

        var argConv = model.GetConversion(arg);
        Assert.True(argConv.IsSpan);
        Assert.True(argConv.IsImplicit);
        Assert.False(argConv.IsUserDefined);
        Assert.False(argConv.IsIdentity);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                System.Span<string> M(int[] arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,41): error CS0029: Cannot implicitly convert type 'int[]' to 'System.Span<string>'
            //     System.Span<string> M(int[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[]", "System.Span<string>").WithLocation(3, 41));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_NullableAnalysis(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            class C
            {
                System.Span<string> M1(string[] arg) => arg;
                System.Span<string> M2(string?[] arg) => arg;
                System.Span<string?> M3(string[] arg) => arg;
                System.Span<string?> M4(string?[] arg) => arg;
                System.Span<int> M5(int?[] arg) => arg;
                System.Span<int?> M6(int[] arg) => arg;
                System.Span<int?> M7(int?[] arg) => arg;
            }
            """;
        var targetType = langVersion > LanguageVersion.CSharp12 ? "System.Span<string>" : "string[]";
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,46): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     System.Span<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", targetType).WithLocation(5, 46),
            // (8,40): error CS0029: Cannot implicitly convert type 'int?[]' to 'System.Span<int>'
            //     System.Span<int> M5(int?[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int?[]", "System.Span<int>").WithLocation(8, 40),
            // (9,40): error CS0029: Cannot implicitly convert type 'int[]' to 'System.Span<int?>'
            //     System.Span<int?> M6(int[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[]", "System.Span<int?>").WithLocation(9, 40));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            class C
            {
                System.ReadOnlySpan<string> M1(string[] arg) => arg;
                System.ReadOnlySpan<string> M2(string?[] arg) => arg;
                System.ReadOnlySpan<string?> M3(string[] arg) => arg;
                System.ReadOnlySpan<string?> M4(string?[] arg) => arg;
                System.ReadOnlySpan<int> M5(int?[] arg) => arg;
                System.ReadOnlySpan<int?> M6(int[] arg) => arg;
                System.ReadOnlySpan<int?> M7(int?[] arg) => arg;
                System.ReadOnlySpan<string> M8(string?[] arg) => arg;
                System.ReadOnlySpan<object> M9(string?[] arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,54): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     System.ReadOnlySpan<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", targetType("string")).WithLocation(5, 54),
            // (8,48): error CS0029: Cannot implicitly convert type 'int?[]' to 'System.ReadOnlySpan<int>'
            //     System.ReadOnlySpan<int> M5(int?[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int?[]", "System.ReadOnlySpan<int>").WithLocation(8, 48),
            // (9,48): error CS0029: Cannot implicitly convert type 'int[]' to 'System.ReadOnlySpan<int?>'
            //     System.ReadOnlySpan<int?> M6(int[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[]", "System.ReadOnlySpan<int?>").WithLocation(9, 48),
            // (11,54): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     System.ReadOnlySpan<string> M8(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", targetType("string")).WithLocation(11, 54),
            // (12,54): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'object[]'.
            //     System.ReadOnlySpan<object> M9(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", targetType("object")).WithLocation(12, 54));

        string targetType(string inner)
            => langVersion > LanguageVersion.CSharp12 ? $"System.ReadOnlySpan<{inner}>" : $"{inner}[]";
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_Nested(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            class C
            {
                System.Span<string[]> M1(string[][] arg) => arg;
                System.Span<string[]> M2(string?[][] arg) => arg;
                System.Span<string?[]> M3(string[][] arg) => arg;
                System.Span<string?[]> M4(string?[][] arg) => arg;
                System.Span<int[]> M5(int?[][] arg) => arg;
                System.Span<int?[]> M6(int[][] arg) => arg;
                System.Span<int?[]> M7(int?[][] arg) => arg;
            }
            """;
        var targetType = langVersion > LanguageVersion.CSharp12 ? "System.Span<string[]>" : "string[][]";
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,50): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     System.Span<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", targetType).WithLocation(5, 50),
            // (8,44): error CS0029: Cannot implicitly convert type 'int?[][]' to 'System.Span<int[]>'
            //     System.Span<int[]> M5(int?[][] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int?[][]", "System.Span<int[]>").WithLocation(8, 44),
            // (9,44): error CS0029: Cannot implicitly convert type 'int[][]' to 'System.Span<int?[]>'
            //     System.Span<int?[]> M6(int[][] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[][]", "System.Span<int?[]>").WithLocation(9, 44));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis_Nested(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            class C
            {
                System.ReadOnlySpan<string[]> M1(string[][] arg) => arg;
                System.ReadOnlySpan<string[]> M2(string?[][] arg) => arg;
                System.ReadOnlySpan<string?[]> M3(string[][] arg) => arg;
                System.ReadOnlySpan<string?[]> M4(string?[][] arg) => arg;
                System.ReadOnlySpan<int[]> M5(int?[][] arg) => arg;
                System.ReadOnlySpan<int?[]> M6(int[][] arg) => arg;
                System.ReadOnlySpan<int?[]> M7(int?[][] arg) => arg;
                System.ReadOnlySpan<string[]> M8(string?[][] arg) => arg;
                System.ReadOnlySpan<object[]> M9(string?[][] arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,58): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     System.ReadOnlySpan<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", targetType("string")).WithLocation(5, 58),
            // (8,52): error CS0029: Cannot implicitly convert type 'int?[][]' to 'System.ReadOnlySpan<int[]>'
            //     System.ReadOnlySpan<int[]> M5(int?[][] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int?[][]", "System.ReadOnlySpan<int[]>").WithLocation(8, 52),
            // (9,52): error CS0029: Cannot implicitly convert type 'int[][]' to 'System.ReadOnlySpan<int?[]>'
            //     System.ReadOnlySpan<int?[]> M6(int[][] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[][]", "System.ReadOnlySpan<int?[]>").WithLocation(9, 52),
            // (11,58): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     System.ReadOnlySpan<string[]> M8(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", targetType("string")).WithLocation(11, 58),
            // (12,58): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'object[][]'.
            //     System.ReadOnlySpan<object[]> M9(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", targetType("object")).WithLocation(12, 58));

        string targetType(string inner)
            => langVersion > LanguageVersion.CSharp12 ? $"System.ReadOnlySpan<{inner}[]>" : $"{inner}[][]";
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_Outer(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            class C
            {
                System.Span<string>? M1(string[] arg) => arg;
                System.ReadOnlySpan<string>? M2(string[] arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,26): error CS0306: The type 'Span<string>' may not be used as a type argument
            //     System.Span<string>? M1(string[] arg) => arg;
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "M1").WithArguments("System.Span<string>").WithLocation(4, 26),
            // (5,34): error CS0306: The type 'ReadOnlySpan<string>' may not be used as a type argument
            //     System.ReadOnlySpan<string>? M2(string[] arg) => arg;
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "M2").WithArguments("System.ReadOnlySpan<string>").WithLocation(5, 34));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_Ref_01(
        [CombinatorialValues("ref", "ref readonly", "in")] string modifier)
    {
        var source = $$"""
            class C
            {
                System.Span<string> M1({{modifier}} string[] arg) => arg;
                System.ReadOnlySpan<string> M2({{modifier}} string[] arg) => arg;
            }
            """;
        var comp = CreateCompilationWithSpan(source);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  ldind.ref
              IL_0002:  newobj     "System.Span<string>..ctor(string[])"
              IL_0007:  ret
            }
            """);
        verifier.VerifyIL("C.M2", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  ldind.ref
              IL_0002:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
              IL_0007:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_Ref_02(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        [CombinatorialValues("ref", "ref readonly", "in")] string modifier)
    {
        var source = $$"""
            using System;

            class C
            {
                Span<string> M1(string[] arg) => M2({{argModifier(modifier)}}
                    arg); // 1
                Span<string> M2({{modifier}} Span<string> arg) => arg;

                ReadOnlySpan<string> M3(string[] arg) => M4({{argModifier(modifier)}}
                    arg); // 2
                ReadOnlySpan<string> M4({{modifier}} ReadOnlySpan<string> arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (6,9): error CS1503: Argument 1: cannot convert from 'ref string[]' to 'ref System.Span<string>'
            //         arg); // 1
            Diagnostic(ErrorCode.ERR_BadArgType, "arg").WithArguments("1", $"{argModifier(modifier)} string[]", $"{modifier} System.Span<string>").WithLocation(6, 9),
            // (10,9): error CS1503: Argument 1: cannot convert from 'ref string[]' to 'ref System.ReadOnlySpan<string>'
            //         arg); // 2
            Diagnostic(ErrorCode.ERR_BadArgType, "arg").WithArguments("1", $"{argModifier(modifier)} string[]", $"{modifier} System.ReadOnlySpan<string>").WithLocation(10, 9));

        static string argModifier(string paramModifier)
            => paramModifier == "ref readonly" ? "in" : paramModifier;
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_Out()
    {
        var source = """
            class C
            {
                System.Span<string> M1(out string[] arg) => arg = null;
                System.ReadOnlySpan<string> M2(out string[] arg) => arg = null;
            }
            """;
        var comp = CreateCompilationWithSpan(source);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size       12 (0xc)
              .maxstack  3
              .locals init (string[] V_0)
              IL_0000:  ldarg.1
              IL_0001:  ldnull
              IL_0002:  dup
              IL_0003:  stloc.0
              IL_0004:  stind.ref
              IL_0005:  ldloc.0
              IL_0006:  newobj     "System.Span<string>..ctor(string[])"
              IL_000b:  ret
            }
            """);
        verifier.VerifyIL("C.M2", """
            {
              // Code size       12 (0xc)
              .maxstack  3
              .locals init (string[] V_0)
              IL_0000:  ldarg.1
              IL_0001:  ldnull
              IL_0002:  dup
              IL_0003:  stloc.0
              IL_0004:  stind.ref
              IL_0005:  ldloc.0
              IL_0006:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_Value()
    {
        var source = """
            class C
            {
                void M()
                {
                    System.Span<string> s = A();
                    System.ReadOnlySpan<string> r = A();
                }
                string[] A() => null;
            }
            """;
        var comp = CreateCompilationWithSpan(source, TestOptions.DebugDll);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       28 (0x1c)
              .maxstack  2
              .locals init (System.Span<string> V_0, //s
                            System.ReadOnlySpan<string> V_1) //r
              IL_0000:  nop
              IL_0001:  ldloca.s   V_0
              IL_0003:  ldarg.0
              IL_0004:  call       "string[] C.A()"
              IL_0009:  call       "System.Span<string>..ctor(string[])"
              IL_000e:  ldloca.s   V_1
              IL_0010:  ldarg.0
              IL_0011:  call       "string[] C.A()"
              IL_0016:  call       "System.ReadOnlySpan<string>..ctor(string[])"
              IL_001b:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                 int[] M(System.Span<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,39): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'int[]'
            //      int[] M(System.Span<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 39));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                 int[] M(System.Span<int> arg) => (int[])arg;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,39): error CS0030: Cannot convert type 'System.Span<int>' to 'int[]'
            //      int[] M(System.Span<int> arg) => (int[])arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 39));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Opposite_Explicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                 int[] M(System.Span<int> arg) => (int[])arg;
            }

            namespace System
            {
                readonly ref struct Span<T>
                {
                    public static explicit operator T[](Span<T> span) => throw null;
                }
            }
            """;
        var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "int[] System.Span<int>.op_Explicit(System.Span<int>)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_Params()
    {
        var source = """
            using System;

            class C
            {
                void M(string[] a)
                {
                    M1(a);
                    M2(a);
                }
                void M1(params Span<string> s) { }
                void M2(params ReadOnlySpan<string> s) { }
            }
            """;
        var comp = CreateCompilationWithSpan(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  newobj     "System.Span<string>..ctor(string[])"
              IL_0007:  call       "void C.M1(params System.Span<string>)"
              IL_000c:  ldarg.0
              IL_000d:  ldarg.1
              IL_000e:  newobj     "System.ReadOnlySpan<string>..ctor(string[])"
              IL_0013:  call       "void C.M2(params System.ReadOnlySpan<string>)"
              IL_0018:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_Multidimensional(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C
            {
                Span<string> M1(string[,] a) => a;
                Span<string> M2(string[][] a) => a;
                Span<string[]> M3(string[,] a) => a;
                Span<string[]> M4(string[][] a) => a;
                Span<string[][]> M5(string[,][] a) => a;
                Span<string[][]> M6(string[][][] a) => a;
                Span<string[,]> M7(string[][][] a) => a;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,37): error CS0029: Cannot implicitly convert type 'string[*,*]' to 'System.Span<string>'
            //     Span<string> M1(string[,] a) => a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("string[*,*]", "System.Span<string>").WithLocation(5, 37),
            // (6,38): error CS0029: Cannot implicitly convert type 'string[][]' to 'System.Span<string>'
            //     Span<string> M2(string[][] a) => a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("string[][]", "System.Span<string>").WithLocation(6, 38),
            // (7,39): error CS0029: Cannot implicitly convert type 'string[*,*]' to 'System.Span<string[]>'
            //     Span<string[]> M3(string[,] a) => a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("string[*,*]", "System.Span<string[]>").WithLocation(7, 39),
            // (9,43): error CS0029: Cannot implicitly convert type 'string[*,*][]' to 'System.Span<string[][]>'
            //     Span<string[][]> M5(string[,][] a) => a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("string[*,*][]", "System.Span<string[][]>").WithLocation(9, 43),
            // (11,43): error CS0029: Cannot implicitly convert type 'string[][][]' to 'System.Span<string[*,*]>'
            //     Span<string[,]> M7(string[][][] a) => a;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "a").WithArguments("string[][][]", "System.Span<string[*,*]>").WithLocation(11, 43));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Covariant(bool cast)
    {
        var source = $$"""
            using System;

            class C
            {
                ReadOnlySpan<object> M(string[] x) => {{(cast ? "(ReadOnlySpan<object>)" : "")}}x;
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size        9 (0x9)
              .maxstack  1
              .locals init (object[] V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])"
              IL_0008:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  newobj     "System.ReadOnlySpan<object>..ctor(object[])"
              IL_0006:  ret
            }
            """;

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Interface_Covariant(bool cast)
    {
        var source = $$"""
            using System;

            class C
            {
                ReadOnlySpan<I<object>> M(I<string>[] x) => {{(cast ? "(ReadOnlySpan<I<object>>)" : "")}}x;
            }

            interface I<out T> { }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size        9 (0x9)
              .maxstack  1
              .locals init (I<object>[] V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  call       "System.ReadOnlySpan<I<object>> System.ReadOnlySpan<I<object>>.op_Implicit(I<object>[])"
              IL_0008:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  newobj     "System.ReadOnlySpan<I<object>>..ctor(I<object>[])"
              IL_0006:  ret
            }
            """;

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Interface_Outside(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        [CombinatorialValues("", "in", "out")] string variance)
    {
        var source = $$"""
            using System;

            class C
            {
                I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            }

            interface I<{{variance}} T> { }
            """;
        // PROTOTYPE: Use `where T : allows ref struct` to get rid of the first error.
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,29): error CS0306: The type 'ReadOnlySpan<object>' may not be used as a type argument
            //     I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            Diagnostic(ErrorCode.ERR_BadTypeArgument, "M").WithArguments("System.ReadOnlySpan<object>").WithLocation(5, 29),
            // (5,49): error CS0266: Cannot implicitly convert type 'I<string[]>' to 'I<System.ReadOnlySpan<object>>'. An explicit conversion exists (are you missing a cast?)
            //     I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<string[]>", "I<System.ReadOnlySpan<object>>").WithLocation(5, 49));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Interface_Invariant(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C
            {
                ReadOnlySpan<I<object>> M(I<string>[] x) => x;
            }

            interface I<T> { }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,49): error CS0029: Cannot implicitly convert type 'I<string>[]' to 'System.ReadOnlySpan<I<object>>'
            //     ReadOnlySpan<I<object>> M(I<string>[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("I<string>[]", "System.ReadOnlySpan<I<object>>").WithLocation(5, 49));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Interface_Contravariant(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C
            {
                ReadOnlySpan<I<object>> M(I<string>[] x) => x;
            }

            interface I<in T> { }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,49): error CS0266: Cannot implicitly convert type 'I<string>[]' to 'System.ReadOnlySpan<I<object>>'. An explicit conversion exists (are you missing a cast?)
            //     ReadOnlySpan<I<object>> M(I<string>[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<string>[]", "System.ReadOnlySpan<I<object>>").WithLocation(5, 49));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Interface_Contravariant_Cast(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M(new[] { new C() })[0].Report();

            class C : I<object>
            {
                public void Report() => Console.Write("C");
                public static ReadOnlySpan<I<object>> M(I<string>[] x) => (ReadOnlySpan<I<object>>)x;
            }

            interface I<in T>
            {
                void Report();
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: "C", verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  castclass  "I<object>[]"
              IL_0006:  call       "System.ReadOnlySpan<I<object>> System.ReadOnlySpan<I<object>>.op_Implicit(I<object>[])"
              IL_000b:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Covariant_ValueType(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C
            {
                ReadOnlySpan<long> M(int[] x) => x;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,38): error CS0029: Cannot implicitly convert type 'int[]' to 'System.ReadOnlySpan<long>'
            //     ReadOnlySpan<long> M(int[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int[]", "System.ReadOnlySpan<long>").WithLocation(5, 38));
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_Covariant_TypeParameter()
    {
        var source = """
            using System;

            class C<T>
            {
                ReadOnlySpan<T> M(T[] x) => x;
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C<T>.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.ReadOnlySpan<T> System.ReadOnlySpan<T>.op_Implicit(T[])"
              IL_0006:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  newobj     "System.ReadOnlySpan<T>..ctor(T[])"
              IL_0006:  ret
            }
            """;

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C<T>.M", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C<T>.M", expectedIl);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Covariant_TypeParameter_NullableValueType_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C<T> where T : struct
            {
                ReadOnlySpan<T> M(T?[] x) => x;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,34): error CS0029: Cannot implicitly convert type 'T?[]' to 'System.ReadOnlySpan<T>'
            //     ReadOnlySpan<T> M(T?[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T?[]", "System.ReadOnlySpan<T>").WithLocation(5, 34));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Covariant_TypeParameter_NullableValueType_02(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C<T> where T : struct
            {
                ReadOnlySpan<T?> M(T[] x) => x;
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,34): error CS0029: Cannot implicitly convert type 'T[]' to 'System.ReadOnlySpan<T?>'
            //     ReadOnlySpan<T?> M(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T[]", "System.ReadOnlySpan<T?>").WithLocation(5, 34));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_ThroughUserImplicit(
        [CombinatorialValues("Span", "ReadOnlySpan")] string destination)
    {
        var source = $$"""
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator int[](C c) => new int[] { 4, 5, 6 };
            }

            static class D
            {
                public static void M({{destination}}<int> xs)
                {
                    foreach (var x in xs)
                    {
                        Console.Write(x);
                    }
                }
            }
            """;

        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.Span<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", $"System.{destination}<int>").WithLocation(3, 5));

        var expectedOutput = "456";

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpan(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Array_Span_ThroughUserImplicit_MissingCtor()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator int[](C c) => new int[] { 4, 5, 6 };
            }

            static class D
            {
                public static void M(Span<int> xs)
                {
                    foreach (var x in xs)
                    {
                        Console.Write(x);
                    }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.Span<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.Span<int>").WithLocation(3, 5)
        };

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.Regular12, expectedDiagnostics);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.Regular12, expectedDiagnostics);

        expectedDiagnostics = [
            // (3,5): error CS0656: Missing compiler required member 'System.Span`1..ctor'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new C()").WithArguments("System.Span`1", ".ctor").WithLocation(3, 5)
        ];

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.RegularNext);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.RegularNext, expectedDiagnostics);

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.RegularPreview);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.RegularPreview, expectedDiagnostics);

        void verifyWithMissing(WellKnownMember member, CSharpParseOptions parseOptions, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithSpan(source, parseOptions: parseOptions);
            comp.MakeMemberMissing(member);
            if (expected.Length == 0)
            {
                CompileAndVerify(comp, expectedOutput: "456").VerifyDiagnostics();
            }
            else
            {
                comp.VerifyDiagnostics(expected);
            }
        }
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_ThroughUserImplicit_MissingCtor()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator int[](C c) => new int[] { 4, 5, 6 };
            }

            static class D
            {
                public static void M(ReadOnlySpan<int> xs)
                {
                    foreach (var x in xs)
                    {
                        Console.Write(x);
                    }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<int>").WithLocation(3, 5)
        };

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.Regular12, expectedDiagnostics);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.Regular12, expectedDiagnostics);

        expectedDiagnostics = [
            // (3,5): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1..ctor'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new C()").WithArguments("System.ReadOnlySpan`1", ".ctor").WithLocation(3, 5)
        ];

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.RegularNext, expectedDiagnostics);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.RegularNext);

        verifyWithMissing(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, TestOptions.RegularPreview, expectedDiagnostics);
        verifyWithMissing(WellKnownMember.System_Span_T__ctor_Array, TestOptions.RegularPreview);

        void verifyWithMissing(WellKnownMember member, CSharpParseOptions parseOptions, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithSpan(source, parseOptions: parseOptions);
            comp.MakeMemberMissing(member);
            if (expected.Length == 0)
            {
                CompileAndVerify(comp, expectedOutput: "456").VerifyDiagnostics();
            }
            else
            {
                comp.VerifyDiagnostics(expected);
            }
        }
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit()
    {
        var source = """
            using System;

            C.M(new int[] { 7, 8, 9 });

            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this Span<int> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (7,40): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<int>)' requires a receiver of type 'System.Span<int>'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", "C.E(System.Span<int>)", "System.Span<int>").WithLocation(7, 40));

        var expectedOutput = "8";

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  newobj     "System.Span<int>..ctor(int[])"
              IL_0006:  call       "void C.E(System.Span<int>)"
              IL_000b:  ret
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Ref(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M(new int[] { 7, 8, 9 });

            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this ref Span<int> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (7,40): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(ref Span<int>)' requires a receiver of type 'ref System.Span<int>'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", "C.E(ref System.Span<int>)", "ref System.Span<int>").WithLocation(7, 40));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_RefReadOnly(
        [CombinatorialValues("ref readonly", "in")] string modifier)
    {
        var source = $$"""
            using System;

            C.M(new int[] { 7, 8, 9 });

            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this {{modifier}} Span<int> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (7,40): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(ref Span<int>)' requires a receiver of type 'ref System.Span<int>'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", $"C.E({modifier} System.Span<int>)", $"{modifier} System.Span<int>").WithLocation(7, 40));

        var expectedOutput = "8";

        var expectedIl = $$"""
            {
              // Code size       15 (0xf)
              .maxstack  1
              .locals init (System.Span<int> V_0)
              IL_0000:  ldarg.0
              IL_0001:  newobj     "System.Span<int>..ctor(int[])"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  call       "void C.E({{modifier}} System.Span<int>)"
              IL_000e:  ret
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Overloads_01()
    {
        var source = """
            using System;

            static class C
            {
                static void M(int[] arg) => arg.E();

                static void E(this Span<int> arg) { }
                static void E(this ReadOnlySpan<int> arg) { }
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (5,33): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<int>)' requires a receiver of type 'System.Span<int>'
            //     static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", "C.E(System.Span<int>)", "System.Span<int>").WithLocation(5, 33));

        var expectedDiagnostics = new[]
        {
            // (5,37): error CS0121: The call is ambiguous between the following methods or properties: 'C.E(Span<int>)' and 'C.E(ReadOnlySpan<int>)'
            //     static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArguments("C.E(System.Span<int>)", "C.E(System.ReadOnlySpan<int>)").WithLocation(5, 37)
        };

        CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpan(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Overloads_02(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M1(default);
            C.M2(default);

            static class C
            {
                public static void M1(Span<int> arg) => arg.E();
                public static void M2(ReadOnlySpan<int> arg) => arg.E();

                static void E(this Span<int> arg) => Console.Write("S ");
                static void E(this ReadOnlySpan<int> arg) => Console.Write("R ");
            }
            """;

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "S R").VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_MissingCtor()
    {
        var source = """
            using System;
            
            C.M(new int[] { 7, 8, 9 });
            
            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this Span<int> arg) => Console.Write(arg[1]);
            }
            """;
        var comp = CreateCompilationWithSpan(source);
        comp.MakeMemberMissing(WellKnownMember.System_Span_T__ctor_Array);
        comp.VerifyDiagnostics(
            // (7,40): error CS0656: Missing compiler required member 'System.Span`1..ctor'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Span`1", ".ctor").WithLocation(7, 40));
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Explicit()
    {
        var source = """
            using System;

            C.M(new int[] { 7, 8, 9 });

            static class C
            {
                public static void M(int[] arg) => ((Span<int>)arg).E();
                public static void E(this Span<int> arg) => Console.Write(arg[1]);
            }
            """;

        var expectedOutput = "8";

        var comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular12);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  call       "void C.E(System.Span<int>)"
              IL_000b:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  newobj     "System.Span<int>..ctor(int[])"
              IL_0006:  call       "void C.E(System.Span<int>)"
              IL_000b:  ret
            }
            """;

        comp = CreateCompilationWithSpan(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpan(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            static class C
            {
                static void M(System.Span<int> arg) => arg.E();
                static void E(this int[] arg) { }
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,44): error CS1929: 'Span<int>' does not contain a definition for 'E' and the best extension method overload 'C.E(int[])' requires a receiver of type 'int[]'
            //     static void M(System.Span<int> arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.Span<int>", "E", "C.E(int[])", "int[]").WithLocation(3, 44));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            static class C
            {
                static void M(System.Span<int> arg) => ((int[])arg).E();
                static void E(this int[] arg) { }
            }
            """;
        CreateCompilationWithSpan(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,45): error CS0030: Cannot convert type 'System.Span<int>' to 'int[]'
            //     static void M(System.Span<int> arg) => ((int[])arg).E();
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 45));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Opposite_Explicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            static class C
            {
                static void M(System.Span<int> arg) => ((int[])arg).E();
                static void E(this int[] arg) { }
            }

            namespace System
            {
                readonly ref struct Span<T>
                {
                    public static explicit operator T[](Span<T> span) => throw null;
                }
            }
            """;
        var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "int[] System.Span<int>.op_Explicit(System.Span<int>)"
              IL_0006:  call       "void C.E(int[])"
              IL_000b:  ret
            }
            """);
    }
}
