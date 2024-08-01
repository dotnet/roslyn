// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
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
            LanguageVersion.CSharp13,
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

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,65): error CS0121: The call is ambiguous between the following methods or properties: 'StringExtensions.Join(string, params string[])' and 'StringExtensions.Join(string, params ReadOnlySpan<string>)'
            //     public static string M(StringValues sv) => StringExtensions.Join(",", sv);
            Diagnostic(ErrorCode.ERR_AmbigCall, "Join").WithArguments("StringExtensions.Join(string, params string[])", "StringExtensions.Join(string, params System.ReadOnlySpan<string>)").WithLocation(7, 65));

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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "Base").VerifyDiagnostics();

        var expectedOutput = "Derived";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "N2").VerifyDiagnostics();

        var expectedOutput = "N1";

        var expectedDiagnostics = new[]
        {
            // (5,5): hidden CS8019: Unnecessary using directive.
            //     using N2;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(5, 5)
        };

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void BreakingChange_ExtensionMethodLookup_SpanVsIEnumerable()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            var arr = new char[] { '/' };
            arr.M('/');

            static class E
            {
                public static void M<T>(this Span<T> s, T x) => Console.Write(1);
                public static void M<T>(this IEnumerable<T> e, T x) => Console.Write(2);
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

        var expectedOutput = "1";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void BreakingChange_ExtensionMethodLookup_SpanVsIEnumerable_Workaround(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Collections.Generic;

            var arr = new char[] { '/' };
            arr.M('/');

            static class E
            {
                public static void M<T>(this Span<T> s, T x) => Console.Write(1);
                public static void M<T>(this IEnumerable<T> e, T x) => Console.Write(2);
                public static void M<T>(this T[] a, T x) => Console.Write(3);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: "3").VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       19 (0x13)
              .maxstack  4
              IL_0000:  ldc.i4.1
              IL_0001:  newarr     "char"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldc.i4.s   47
              IL_000a:  stelem.i2
              IL_000b:  ldc.i4.s   47
              IL_000d:  call       "void E.M<char>(char[], char)"
              IL_0012:  ret
            }
            """);
    }

    [Fact]
    public void BreakingChange_ExtensionMethodLookup_SpanVsIEnumerable_MethodConversion()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            var arr = new int[] { 123 };
            E.R(arr.M);

            static class E
            {
                public static void R(Action<int> a) => a(-1);
                public static void M<T>(this Span<T> s, T x) => Console.Write(1);
                public static void M<T>(this IEnumerable<T> e, T x) => Console.Write(2);
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "2").VerifyDiagnostics();

        // PROTOTYPE: Can we avoid this break?

        var expectedDiagnostics = new[]
        {
            // (5,5): error CS1113: Extension method 'E.M<int>(Span<int>, int)' defined on value type 'Span<int>' cannot be used to create delegates
            // E.R(arr.M);
            Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "arr.M").WithArguments("E.M<int>(System.Span<int>, int)", "System.Span<int>").WithLocation(5, 5)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void BreakingChange_ExtensionMethodLookup_SpanVsIEnumerable_MethodConversion_Workaround(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Collections.Generic;
            
            var arr = new int[] { 123 };
            E.R(arr.M);
            
            static class E
            {
                public static void R(Action<int> a) => a(-1);
                public static void M<T>(this Span<T> s, T x) => Console.Write(1);
                public static void M<T>(this IEnumerable<T> e, T x) => Console.Write(2);
                public static void M<T>(this T[] a, T x) => Console.Write(3);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "3").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit(
        [CombinatorialLangVersions] LanguageVersion langVersion,
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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
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
    }

    [Theory, CombinatorialData]
    public void Conversion_Span_ReadOnlySpan_Implicit(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<int> s = {{(cast ? "(ReadOnlySpan<int>)" : "")}}source();
            report(s);
            static Span<int> source() => new Span<int>(new int[] { 1, 2, 3 });
            static void report(ReadOnlySpan<int> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var expectedOutput = "123";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "System.Span<int> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<int>)"
              IL_000f:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit(bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = {{(cast ? "(ReadOnlySpan<object>)" : "")}}source();
            report(s);
            static Span<string> source() => new Span<string>(new[] { "a", "b" });
            static void report(ReadOnlySpan<object> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        if (!cast)
        {
            comp.VerifyDiagnostics(
                // (2,26): error CS0029: Cannot implicitly convert type 'System.Span<string>' to 'System.ReadOnlySpan<object>'
                // ReadOnlySpan<object> s = source();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));
        }
        else
        {
            comp.VerifyDiagnostics(
                // (2,26): error CS0030: Cannot convert type 'System.Span<string>' to 'System.ReadOnlySpan<object>'
                // ReadOnlySpan<object> s = (ReadOnlySpan<object>)source();
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<object>)source()").WithArguments("System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));
        }

        var expectedOutput = "ab";

        var expectedIL = """
            {
              // Code size       21 (0x15)
              .maxstack  1
              IL_0000:  call       "System.Span<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)"
              IL_000a:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000f:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<object>)"
              IL_0014:  ret
            }
            """;

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
    }

    [Theory, CombinatorialData]
    public void Conversion_Span_ReadOnlySpan_Implicit_IdentityInnerConversion(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = {{(cast ? "(ReadOnlySpan<object>)" : "")}}source();
            report(s);
            static Span<dynamic> source() => new Span<dynamic>(new object[] { "a", "b" });
            static void report(ReadOnlySpan<object> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var expectedOutput = "ab";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "System.Span<dynamic> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<dynamic> System.Span<dynamic>.op_Implicit(System.Span<dynamic>)"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<object>)"
              IL_000f:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit(bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = {{(cast ? "(ReadOnlySpan<object>)" : "")}}source();
            report(s);
            static ReadOnlySpan<string> source() => new ReadOnlySpan<string>(new[] { "a", "b" });
            static void report(ReadOnlySpan<object> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        if (!cast)
        {
            comp.VerifyDiagnostics(
                // (2,26): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<string>' to 'System.ReadOnlySpan<object>'
                // ReadOnlySpan<object> s = source();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.ReadOnlySpan<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));
        }
        else
        {
            comp.VerifyDiagnostics(
                // (2,26): error CS0030: Cannot convert type 'System.ReadOnlySpan<string>' to 'System.ReadOnlySpan<object>'
                // ReadOnlySpan<object> s = (ReadOnlySpan<object>)source();
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<object>)source()").WithArguments("System.ReadOnlySpan<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));
        }

        var expectedOutput = "ab";

        var expectedIL = """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<object>)"
              IL_000f:  ret
            }
            """;

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
    }

    [Theory, CombinatorialData]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_IdentityInnerConversion(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = {{(cast ? "(ReadOnlySpan<object>)" : "")}}source();
            report(s);
            static ReadOnlySpan<dynamic> source() => new ReadOnlySpan<dynamic>(new string[] { "a", "b" });
            static void report(ReadOnlySpan<object> s) { foreach (var x in s) { Console.Write(x); } }
            """;

        var expectedOutput = "ab";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       11 (0xb)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<dynamic> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<object>)"
              IL_000a:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_string_ReadOnlySpan_Implicit(bool cast)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<char> s = {{(cast ? "(ReadOnlySpan<char>)" : "")}}source();
            report(s);
            static string source() => "abc";
            static void report(ReadOnlySpan<char> s) { foreach (var x in s) { Console.Write(x + " "); } }
            """;

        var expectedOutput = "a b c";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        var owner = ExecutionConditionUtil.IsCoreClr ? "string" : "System.ReadOnlySpan<char>";
        verifier.VerifyIL("<top-level-statements-entry-point>", $$"""
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> {{owner}}.op_Implicit(string)"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<char>)"
              IL_000f:  ret
            }
            """);

        var expectedIL = """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000a:  call       "void Program.<<Main>$>g__report|0_1(System.ReadOnlySpan<char>)"
              IL_000f:  ret
            }         
            """;

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIL);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MissingHelper()
    {
        var source = """
            using System;
            Span<int> s = arr();
            static int[] arr() => throw null;

            namespace System
            {
                public readonly ref struct Span<T>
                {
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,15): error CS0029: Cannot implicitly convert type 'int[]' to 'System.Span<int>'
            // Span<int> s = arr();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arr()").WithArguments("int[]", "System.Span<int>").WithLocation(2, 15));

        var expectedDiagnostics = new[]
        {
            // (2,15): error CS0656: Missing compiler required member 'System.Span<T>.op_Implicit'
            // Span<int> s = arr();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arr()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 15)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_MissingHelper()
    {
        var source = """
            using System;
            ReadOnlySpan<int> s = source();
            static Span<int> source() => throw null;

            namespace System
            {
                public readonly ref struct Span<T> { }
                public readonly ref struct ReadOnlySpan<T> { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,23): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'System.ReadOnlySpan<int>'
            // ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.Span<int>", "System.ReadOnlySpan<int>").WithLocation(2, 23));

        var expectedDiagnostics = new[]
        {
            // (2,23): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 23)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_MissingHelper_01()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static Span<string> source() => throw null;

            namespace System
            {
                public readonly ref struct Span<T> { }
                public readonly ref struct ReadOnlySpan<T> { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,26): error CS0029: Cannot implicitly convert type 'System.Span<string>' to 'System.ReadOnlySpan<object>'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));

        var expectedDiagnostics = new[]
        {
            // (2,26): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 26),
            // (2,26): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 26)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_MissingHelper_02()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static Span<string> source() => throw null;

            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator ReadOnlySpan<T>(Span<T> s) => throw null;
                }
                public readonly ref struct ReadOnlySpan<T> { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,26): error CS0029: Cannot implicitly convert type 'System.Span<string>' to 'System.ReadOnlySpan<object>'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));

        var expectedDiagnostics = new[]
        {
            // (2,26): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 26)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_MissingHelper_03()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static Span<string> source() => throw null;

            namespace System
            {
                public readonly ref struct Span<T> { }
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> s) where TDerived : class, T => throw null;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,26): error CS0029: Cannot implicitly convert type 'System.Span<string>' to 'System.ReadOnlySpan<object>'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));

        var expectedDiagnostics = new[]
        {
            // (2,26): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 26)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MissingHelper()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static ReadOnlySpan<string> source() => throw null;

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T> { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,26): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<string>' to 'System.ReadOnlySpan<object>'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("System.ReadOnlySpan<string>", "System.ReadOnlySpan<object>").WithLocation(2, 26));

        var expectedDiagnostics = new[]
        {
            // (2,26): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 26)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_MissingHelper()
    {
        var source = """
            using System;
            ReadOnlySpan<char> s = source();
            static string source() => throw null;

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T> { }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (2,24): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<char>'
            // ReadOnlySpan<char> s = source();
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "source()").WithArguments("string", "System.ReadOnlySpan<char>").WithLocation(2, 24));

        var expectedDiagnostics = new[]
        {
            // (2,24): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            // ReadOnlySpan<char> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(2, 24)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_DifferentOperator(
        [CombinatorialValues("int[]", "T[][]")] string parameterType)
    {
        var source = $$"""
            using System;
            Span<int> s = arr();
            static int[] arr() => new int[] { 1, 2, 3 };

            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator Span<T>({{parameterType}} array) => throw null;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,15): error CS0656: Missing compiler required member 'System.Span<T>.op_Implicit'
            // Span<int> s = arr();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arr()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 15));
    }

    [Theory]
    [InlineData("ReadOnlySpan<char> AsSpan(string s)", true)]
    [InlineData("ReadOnlySpan<int> AsSpan(string s)", false)]
    [InlineData("ReadOnlySpan<T> AsSpan<T>(string s)", false)]
    [InlineData("ReadOnlySpan<char> AsSpan(object o)", false)]
    [InlineData("Span<char> AsSpan(string s)", false)]
    public void Conversion_string_ReadOnlySpan_Implicit_DifferentHelper(string signature, bool works)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<char> s = source();
            static string source() => "";

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T> { }
                public readonly ref struct Span<T> { }
                public static class MemoryExtensions
                {
                    public static {{signature}} => default;
                }
            }
            """;
        var comp = CreateCompilation(source);
        if (works)
        {
            // Make sure the code above can work so we are testing something useful.
            comp.VerifyDiagnostics();
        }
        else
        {
            comp.VerifyDiagnostics(
                // (2,24): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                // ReadOnlySpan<char> s = source();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(2, 24));
        }
    }

    [Theory, CombinatorialData]
    public void Conversion_string_ReadOnlySpan_Implicit_HelperAccessibility_01(
        [CombinatorialValues("public", "internal")] string classModifier,
        [CombinatorialValues("public", "internal")] string methodModifier)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<char> s = source();
            static string source() => "";

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T> { }
                {{classModifier}} static class MemoryExtensions
                {
                    {{methodModifier}} static ReadOnlySpan<char> AsSpan(string s) => default;
                }
            }
            """;
        var comp = CreateCompilation(source);
        if (methodModifier == "internal")
        {
            comp.VerifyDiagnostics(
                // (2,24): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                // ReadOnlySpan<char> s = source();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(2, 24));
        }
        else
        {
            comp.VerifyDiagnostics();
        }
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_HelperAccessibility_02()
    {
        var source1 = """
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            internal static class MemoryExtensions
            {
                public static ReadOnlySpan<char> AsSpan(string s) => default;
            }
            """;
        var comp1 = CreateCompilation(source1).VerifyDiagnostics().EmitToImageReference();

        var source2 = """
            using System;
            ReadOnlySpan<char> s1 = source();
            ReadOnlySpan<char> s2 = MemoryExtensions.AsSpan(source());
            static string source() => "";
            """;
        CreateCompilation(source2, [comp1]).VerifyDiagnostics(
            // (3,25): error CS0122: 'MemoryExtensions' is inaccessible due to its protection level
            // ReadOnlySpan<char> s2 = MemoryExtensions.AsSpan(source());
            Diagnostic(ErrorCode.ERR_BadAccess, "MemoryExtensions").WithArguments("System.MemoryExtensions").WithLocation(3, 25));
    }

    [Theory, CombinatorialData]
    public void Conversion_string_ReadOnlySpan_Implicit_RedefinedHelper(
        [CombinatorialValues("public", "internal")] string classModifier,
        [CombinatorialValues("public", "internal")] string methodModifier)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<char> s = source();
            static string source() => "";

            namespace System
            {
                {{classModifier}} static class MemoryExtensions
                {
                    {{methodModifier}} static ReadOnlySpan<char> AsSpan(string s) => default;
                }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        if (methodModifier == "public")
        {
            comp.VerifyDiagnostics(
                // (2,24): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
                // ReadOnlySpan<char> s = source();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(2, 24));
        }
        else
        {
            comp.VerifyDiagnostics();
        }
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_ExplicitOperator()
    {
        var source = """
            using System;
            Span<int> s = arr();
            static int[] arr() => new int[] { 1, 2, 3 };

            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static explicit operator Span<T>(T[] array) => throw null;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,15): error CS0656: Missing compiler required member 'System.Span<T>.op_Implicit'
            // Span<int> s = arr();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arr()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 15));
    }

    [Fact]
    public void Conversion_Array_Span_Explicit_ExplicitOperator()
    {
        var source = """
            using System;
            Span<int> s = (Span<int>)arr();
            static int[] arr() => new int[] { 1, 2, 3 };

            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static explicit operator Span<T>(T[] array) => throw null;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,15): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // Span<int> s = (Span<int>)arr();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(Span<int>)arr()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 15));
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_UnrecognizedModreq()
    {
        /*
            public struct Span<T>
            {
                public static implicit operator modreq(A) Span<T>(T[] array) => throw null;
            }
            public class A { }
            public static class C
            {
                public static void M(Span<int> s) { }
            }
         */
        var ilSource = """
            .class public sequential ansi sealed beforefieldinit System.Span`1<T> extends System.ValueType
            {
                .pack 0
                .size 1
                .method public hidebysig specialname static valuetype System.Span`1<!T> modreq(A) op_Implicit(!T[] 'array') cil managed
                {
                    .maxstack 1
                    ret
                }
            }
            .class public auto ansi sealed beforefieldinit A extends System.Object
            {
            }
            .class public auto ansi abstract sealed beforefieldinit C extends System.Object
            {
                .method public hidebysig static void M(valuetype System.Span`1<int32> s) cil managed 
                {
                    .maxstack 1
                    ret
                }
            }
            """;
        var source = """
            C.M(new int[] { 1, 2, 3 });
            """;
        CreateCompilationWithIL(source, ilSource).VerifyDiagnostics(
            // (1,5): error CS0570: 'Span<T>.implicit operator Span<T>(T[])' is not supported by the language
            // C.M(new int[] { 1, 2, 3 });
            Diagnostic(ErrorCode.ERR_BindToBogus, "new int[] { 1, 2, 3 }").WithArguments("System.Span<T>.implicit operator System.Span<T>(T[])").WithLocation(1, 5));
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_UnrecognizedModopt()
    {
        /*
            public struct Span<T>
            {
                public static implicit operator modopt(A) Span<T>(T[] array) => throw null;
            }
            public class A { }
            public static class C
            {
                public static void M(Span<int> s) { }
            }
         */
        var ilSource = """
            .class public sequential ansi sealed beforefieldinit System.Span`1<T> extends System.ValueType
            {
                .pack 0
                .size 1
                .method public hidebysig specialname static valuetype System.Span`1<!T> modopt(A) op_Implicit(!T[] 'array') cil managed
                {
                    .maxstack 1
                    ret
                }
            }
            .class public auto ansi sealed beforefieldinit A extends System.Object
            {
            }
            .class public auto ansi abstract sealed beforefieldinit C extends System.Object
            {
                .method public hidebysig static void M(valuetype System.Span`1<int32> s) cil managed 
                {
                    .maxstack 1
                    ret
                }
            }
            """;
        var source = """
            C.M(arr());
            static int[] arr() => new int[] { 1, 2, 3 };
            """;
        var comp = CreateCompilationWithIL(source, ilSource);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_000a:  call       "void C.M(System.Span<int>)"
              IL_000f:  ret
            }
            """);
    }

    [Theory]
    [InlineData("where TDerived : T")]
    [InlineData("")]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MissingClassConstraint(string constraints)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = source();
            static ReadOnlySpan<string> source() => throw null;
            
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> items) {{constraints}} => throw null;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,26): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 26));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MissingInheritsConstraint()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static ReadOnlySpan<string> source() => throw null;
            
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> items) where TDerived : class => throw null;
                }
            }
            """;
        var verifier = CompileAndVerify(source);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_AdditionalConstraint()
    {
        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            static ReadOnlySpan<string> source() => throw null;

            public class C { }
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> items) where TDerived : C => throw null;
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (2,26): error CS0311: The type 'string' cannot be used as type parameter 'TDerived' in the generic type or method 'ReadOnlySpan<object>.CastUp<TDerived>(ReadOnlySpan<TDerived>)'. There is no implicit reference conversion from 'string' to 'C'.
            // ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "source()").WithArguments("System.ReadOnlySpan<object>.CastUp<TDerived>(System.ReadOnlySpan<TDerived>)", "C", "TDerived", "string").WithLocation(2, 26));
    }

    [Theory]
    [InlineData("T", "T", false)]
    [InlineData("T", "TDerived", false)]
    [InlineData("TDerived", "TDerived", false)]
    [InlineData("TDerived", "T", true)]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_WrongTypeArgument(string arg, string result, bool works)
    {
        var source = $$"""
            using System;
            ReadOnlySpan<object> s = source();
            static ReadOnlySpan<string> source() => throw null;

            public class C { }
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<{{result}}> CastUp<TDerived>(ReadOnlySpan<{{arg}}> items) where TDerived : class, T => throw null;
                }
            }
            """;
        var comp = CreateCompilation(source);
        if (works)
        {
            // Make sure the code above can work so we are testing something useful.
            comp.VerifyDiagnostics();
        }
        else
        {
            comp.VerifyDiagnostics(
                // (2,26): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
                // ReadOnlySpan<object> s = source();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 26));
        }
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_ConstantData(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M1(new[] { 1 });
            C.M2(new[] { 2 });

            static class C
            {
                public static void M1(Span<int> s) => Console.Write(s[0]);
                public static void M2(ReadOnlySpan<int> s) => Console.Write(s[0]);
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
        if (ExecutionConditionUtil.IsCoreClr)
        {
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       36 (0x24)
                  .maxstack  4
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  stelem.i4
                  IL_000a:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
                  IL_000f:  call       "void C.M1(System.Span<int>)"
                  IL_0014:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=4_Align=4 <PrivateImplementationDetails>.26B25D457597A7B0463F9620F666DD10AA2C4373A505967C7C8D70922A2D6ECE4"
                  IL_0019:  call       "System.ReadOnlySpan<int> System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<int>(System.RuntimeFieldHandle)"
                  IL_001e:  call       "void C.M2(System.ReadOnlySpan<int>)"
                  IL_0023:  ret
                }
                """);
        }
        else
        {
            verifier.VerifyIL("<top-level-statements-entry-point>", """
                {
                  // Code size       63 (0x3f)
                  .maxstack  4
                  IL_0000:  ldc.i4.1
                  IL_0001:  newarr     "int"
                  IL_0006:  dup
                  IL_0007:  ldc.i4.0
                  IL_0008:  ldc.i4.1
                  IL_0009:  stelem.i4
                  IL_000a:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
                  IL_000f:  call       "void C.M1(System.Span<int>)"
                  IL_0014:  ldsfld     "int[] <PrivateImplementationDetails>.26B25D457597A7B0463F9620F666DD10AA2C4373A505967C7C8D70922A2D6ECE_A6"
                  IL_0019:  dup
                  IL_001a:  brtrue.s   IL_0034
                  IL_001c:  pop
                  IL_001d:  ldc.i4.1
                  IL_001e:  newarr     "int"
                  IL_0023:  dup
                  IL_0024:  ldtoken    "int <PrivateImplementationDetails>.26B25D457597A7B0463F9620F666DD10AA2C4373A505967C7C8D70922A2D6ECE"
                  IL_0029:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                  IL_002e:  dup
                  IL_002f:  stsfld     "int[] <PrivateImplementationDetails>.26B25D457597A7B0463F9620F666DD10AA2C4373A505967C7C8D70922A2D6ECE_A6"
                  IL_0034:  newobj     "System.ReadOnlySpan<int>..ctor(int[])"
                  IL_0039:  call       "void C.M2(System.ReadOnlySpan<int>)"
                  IL_003e:  ret
                }
                """);
        }
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_ConstantData_NotWellKnownSpan(LanguageVersion langVersion)
    {
        var source = """
            extern alias span;
            C.M1(new[] { 1 });
            C.M2(new[] { 2 });
            static class C
            {
                public static void M1(span::System.Span<int> s) => System.Console.Write(s[0]);
                public static void M2(span::System.ReadOnlySpan<int> s) => System.Console.Write(s[0]);
            }
            """;
        var spanDll = CreateCompilation(TestSources.Span, options: TestOptions.UnsafeReleaseDll)
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span"]);
        var verifier = CompileAndVerify([source, TestSources.Span],
            references: [spanDll],
            expectedOutput: "12",
            verify: Verification.Fails,
            // warning CS0436: Type conflicts with imported type
            options: TestOptions.UnsafeReleaseExe.WithSpecificDiagnosticOptions("CS0436", ReportDiagnostic.Suppress),
            parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       41 (0x29)
              .maxstack  4
              IL_0000:  ldc.i4.1
              IL_0001:  newarr     "int"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldc.i4.1
              IL_0009:  stelem.i4
              IL_000a:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_000f:  call       "void C.M1(System.Span<int>)"
              IL_0014:  ldc.i4.1
              IL_0015:  newarr     "int"
              IL_001a:  dup
              IL_001b:  ldc.i4.0
              IL_001c:  ldc.i4.2
              IL_001d:  stelem.i4
              IL_001e:  call       "System.ReadOnlySpan<int> System.ReadOnlySpan<int>.op_Implicit(int[])"
              IL_0023:  call       "void C.M2(System.ReadOnlySpan<int>)"
              IL_0028:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_MultipleSpans_01(
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct {{type}}<T>
                {
                    public static implicit operator {{type}}<T>(T[] array)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = $$"""
            using System;
            {{type}}<int> s = arr();
            use(s);
            static int[] arr() => new int[] { 1, 2, 3 };
            static void use({{type}}<int> s) { }
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Internal");
        verifier.VerifyDiagnostics(
            // (2,1): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
            // Span<int> s = arr();
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, $"{type}<int>").WithArguments("", $"System.{type}<T>", "Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", $"System.{type}<T>").WithLocation(2, 1),
            // (5,17): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
            // static void use(Span<int> s) { }
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, $"{type}<int>").WithArguments("", $"System.{type}<T>", "Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", $"System.{type}<T>").WithLocation(5, 17),
            // (5,41): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
            //         public static implicit operator Span<T>(T[] array)
            Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, $"{type}<T>").WithArguments("", $"System.{type}<T>", "Span1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", $"System.{type}<T>").WithLocation(5, 41));

        verifier.VerifyIL("<top-level-statements-entry-point>", $$"""
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  call       "System.{{type}}<int> System.{{type}}<int>.op_Implicit(int[])"
              IL_000a:  call       "void Program.<<Main>$>g__use|0_1(System.{{type}}<int>)"
              IL_000f:  ret
            }
            """);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_MultipleSpans_02(
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct {{type}}<T>
                {
                    public static implicit operator {{type}}<T>(T[] array)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["lib"]);

        var source = $$"""
            extern alias lib;
            lib::System.{{type}}<int> s = arr();
            static int[] arr() => new int[] { 1, 2, 3 };
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "External");
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", $$"""
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "int[] Program.<<Main>$>g__arr|0_0()"
              IL_0005:  call       "System.{{type}}<int> System.{{type}}<int>.op_Implicit(int[])"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_MultipleSpans_01()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>;
                public readonly ref struct Span<T>
                {
                    public static implicit operator ReadOnlySpan<T>(Span<T> span)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = """
            using System;
            ReadOnlySpan<int> s = source();
            use(s);
            static Span<int> source() => default;
            static void use(ReadOnlySpan<int> s) { }
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer",
            // warning CS0436: Type conflicts with imported type
            options: TestOptions.UnsafeReleaseExe.WithSpecificDiagnosticOptions("CS0436", ReportDiagnostic.Suppress));
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Internal");
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "System.Span<int> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)"
              IL_000a:  call       "void Program.<<Main>$>g__use|0_1(System.ReadOnlySpan<int>)"
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_MultipleSpans_02()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>;
                public readonly ref struct Span<T>
                {
                    public static implicit operator ReadOnlySpan<T>(Span<T> span)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["lib"]);

        var source = """
            extern alias lib;
            lib::System.ReadOnlySpan<int> s = source();
            static lib::System.Span<int> source() => default;
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "External");
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "System.Span<int> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);

        source = """
            extern alias lib;
            lib::System.ReadOnlySpan<int> s = source();
            static System.Span<int> source() => default;
            """;
        CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,35): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // lib::System.ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 35));

        source = """
            extern alias lib;
            System.ReadOnlySpan<int> s = source();
            static lib::System.Span<int> source() => default;
            """;
        CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,30): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // System.ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 30));
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_MultipleSpans_03()
    {
        var span1 = CreateCompilation("""
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            public readonly ref struct Span<T>;
            """, assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span1"]);
        var span2 = CreateCompilation("""
            extern alias span1;
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            public readonly ref struct Span<T>
            {
                public static implicit operator span1::System.ReadOnlySpan<T>(Span<T> span)
                {
                    Console.Write("Span2");
                    return default;
                }
            }
            """, [span1], assemblyName: "Span2")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span2"]);

        var source = """
            extern alias span1;
            extern alias span2;
            span1::System.ReadOnlySpan<int> s = source();
            static span2::System.Span<int> source() => default;
            """;

        var comp = CreateCompilation(source, [span1, span2], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Span2");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "System.Span<int> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);

        source = """
            extern alias span2;
            span2::System.ReadOnlySpan<int> s = source();
            static span2::System.Span<int> source() => default;
            """;
        CreateCompilation(source, [span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,37): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // span2::System.ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(2, 37));

        source = """
            extern alias span1;
            extern alias span2;
            span2::System.ReadOnlySpan<int> s = source();
            static span1::System.Span<int> source() => default;
            """;
        CreateCompilation(source, [span1, span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (3,37): error CS0656: Missing compiler required member 'Span<T>.op_Implicit'
            // span2::System.ReadOnlySpan<int> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(3, 37));
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_MultipleSpans()
    {
        var span1 = CreateCompilation("""
            namespace System;
            public readonly ref struct Span<T>;
            public readonly ref struct ReadOnlySpan<T>
            {
                public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> span) where TDerived : class, T
                {
                    Console.WriteLine("CastUp");
                    return default;
                }
            }
            """, assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span1"]);
        var span2 = CreateCompilation("""
            extern alias span1;
            namespace System;
            public readonly ref struct Span<T>
            {
                public static implicit operator span1::System.ReadOnlySpan<T>(Span<T> span)
                {
                    Console.WriteLine("Span2 to ReadOnlySpan1");
                    return default;
                }
            }
            """, [span1], assemblyName: "Span2")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span2"]);

        var source = """
            extern alias span1;
            extern alias span2;
            span1::System.ReadOnlySpan<object> s = source();
            static span2::System.Span<string> source() => default;
            """;

        var comp = CreateCompilation(source, [span1, span2], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: """
            Span2 to ReadOnlySpan1
            CastUp
            """);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       17 (0x11)
              .maxstack  1
              IL_0000:  call       "System.Span<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)"
              IL_000a:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000f:  pop
              IL_0010:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MultipleSpans_01()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> other) where TDerived : class, T
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = """
            using System;
            ReadOnlySpan<object> s = source();
            use(s);
            static ReadOnlySpan<string> source() => default;
            static void use(ReadOnlySpan<object> s) { }
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer",
            // warning CS0436: Type conflicts with imported type
            options: TestOptions.UnsafeReleaseExe.WithSpecificDiagnosticOptions("CS0436", ReportDiagnostic.Suppress));
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Internal");
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000a:  call       "void Program.<<Main>$>g__use|0_1(System.ReadOnlySpan<object>)"
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MultipleSpans_02()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> other) where TDerived : class, T
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["lib"]);

        var source = """
            extern alias lib;
            lib::System.ReadOnlySpan<object> s = source();
            static lib::System.ReadOnlySpan<string> source() => default;
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "External");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);

        source = """
            extern alias lib;
            lib::System.ReadOnlySpan<object> s = source();
            static System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,38): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // lib::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 38));

        source = """
            extern alias lib;
            System.ReadOnlySpan<object> s = source();
            static lib::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,33): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 33));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MultipleSpans_03()
    {
        var span1 = CreateCompilation("""
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            """, assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span1"]);
        var span2 = CreateCompilation("""
            extern alias span1;
            namespace System;
            public readonly ref struct ReadOnlySpan<T>
            {
                public static span1::System.ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> other) where TDerived : class, T
                {
                    Console.Write("Span2");
                    return default;
                }
            }
            """, [span1], assemblyName: "Span2")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span2"]);

        var source = """
            extern alias span1;
            extern alias span2;
            span1::System.ReadOnlySpan<object> s = source();
            static span2::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation(source, [span1, span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (3,40): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // span1::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(3, 40));

        source = """
            extern alias span2;
            span2::System.ReadOnlySpan<object> s = source();
            static span2::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation(source, [span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,40): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // span2::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 40));

        source = """
            extern alias span1;
            extern alias span2;
            span2::System.ReadOnlySpan<object> s = source();
            static span1::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation(source, [span1, span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (3,40): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // span2::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(3, 40));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_MultipleSpans_04()
    {
        var span1 = CreateCompilation("""
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            """, assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span1"]);
        var span2 = CreateCompilation("""
            extern alias span1;
            namespace System;
            public readonly ref struct ReadOnlySpan<T>
            {
                public static ReadOnlySpan<T> CastUp<TDerived>(span1::System.ReadOnlySpan<TDerived> other) where TDerived : class, T
                {
                    Console.Write("Span2");
                    return default;
                }
            }
            """, [span1], assemblyName: "Span2")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span2"]);

        var source = """
            extern alias span1;
            extern alias span2;
            span1::System.ReadOnlySpan<object> s = source();
            static span2::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation(source, [span1, span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (3,40): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // span1::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(3, 40));

        source = """
            extern alias span2;
            span2::System.ReadOnlySpan<object> s = source();
            static span2::System.ReadOnlySpan<string> source() => default;
            """;
        CreateCompilation(source, [span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,40): error CS0656: Missing compiler required member 'ReadOnlySpan<T>.CastUp'
            // span2::System.ReadOnlySpan<object> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.ReadOnlySpan<T>", "CastUp").WithLocation(2, 40));

        source = """
            extern alias span1;
            extern alias span2;
            span2::System.ReadOnlySpan<object> s = source();
            static span1::System.ReadOnlySpan<string> source() => default;
            """;
        var comp = CreateCompilation(source, [span1, span2], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Span2");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "System.ReadOnlySpan<string> Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_MultipleSpans_01()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>;
                public static class MemoryExtensions
                {
                    public static ReadOnlySpan<char> AsSpan(string s)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference();

        var source = """
            using System;
            ReadOnlySpan<char> s = source();
            use(s);
            static string source() => "";
            static void use(ReadOnlySpan<char> s) { }
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer",
            // warning CS0436: Type conflicts with imported type
            options: TestOptions.UnsafeReleaseExe.WithSpecificDiagnosticOptions("CS0436", ReportDiagnostic.Suppress));
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Internal");
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       16 (0x10)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000a:  call       "void Program.<<Main>$>g__use|0_1(System.ReadOnlySpan<char>)"
              IL_000f:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_MultipleSpans_02()
    {
        static string getSpanSource(string output) => $$"""
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>;
                public static class MemoryExtensions
                {
                    public static ReadOnlySpan<char> AsSpan(string s)
                    {
                        Console.Write("{{output}}");
                        return default;
                    }
                }
            }
            """;

        var spanComp = CreateCompilation(getSpanSource("External"), assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["lib"]);

        var source = """
            extern alias lib;
            lib::System.ReadOnlySpan<char> s = source();
            static string source() => "";
            """;

        var comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "External");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);

        source = """
            System.ReadOnlySpan<char> s = source();
            static string source() => "";
            """;

        comp = CreateCompilation([source, getSpanSource("Internal")], [spanComp], assemblyName: "Consumer");
        verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify, expectedOutput: "Internal");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_MultipleSpans_03()
    {
        var span1 = CreateCompilation("""
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            """, assemblyName: "Span1")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span1"]);
        var span2 = CreateCompilation($$"""
            extern alias span1;
            namespace System;
            public readonly ref struct ReadOnlySpan<T>;
            public static class MemoryExtensions
            {
                public static span1::System.ReadOnlySpan<char> AsSpan(string s)
                {
                    Console.Write("Span2");
                    return default;
                }
            }
            """, [span1], assemblyName: "Span2")
            .VerifyDiagnostics()
            .EmitToImageReference(aliases: ["span2"]);

        var source = """
            extern alias span1;
            span1::System.ReadOnlySpan<char> s = source();
            static string source() => "";
            """;
        var comp = CreateCompilation(source, [span1, span2], assemblyName: "Consumer");
        var verifier = CompileAndVerify(comp, expectedOutput: "Span2");
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  call       "string Program.<<Main>$>g__source|0_0()"
              IL_0005:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000a:  pop
              IL_000b:  ret
            }
            """);

        source = """
            extern alias span2;
            span2::System.ReadOnlySpan<char> s = source();
            static string source() => "";
            """;
        CreateCompilation(source, [span2], assemblyName: "Consumer").VerifyDiagnostics(
            // (2,38): error CS0656: Missing compiler required member 'System.MemoryExtensions.AsSpan'
            // span2::System.ReadOnlySpan<char> s = source();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "source()").WithArguments("System.MemoryExtensions", "AsSpan").WithLocation(2, 38));
    }

    [Theory]
    [InlineData("System.Int32[]", "System.Span<System.Int32>")]
    [InlineData("System.Int32[]", "System.ReadOnlySpan<System.Int32>")]
    [InlineData("System.Span<System.Int32>", "System.ReadOnlySpan<System.Int32>")]
    [InlineData("System.ReadOnlySpan<System.String>", "System.ReadOnlySpan<System.Object>")]
    [InlineData("System.String", "System.ReadOnlySpan<System.Char>")]
    public void Conversion_Span_Implicit_SemanticModel(string sourceType, string targetType)
    {
        var source = $$"""
            class C
            {
                {{targetType}} M({{sourceType}} arg) { return arg; }
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var arg = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
        Assert.Equal("arg", arg!.ToString());

        var argType = model.GetTypeInfo(arg);
        Assert.Equal(sourceType, argType.Type.ToTestDisplayString());
        Assert.Equal(targetType, argType.ConvertedType.ToTestDisplayString());

        var argConv = model.GetConversion(arg);
        Assert.Equal(ConversionKind.ImplicitSpan, argConv.Kind);
        Assert.True(argConv.IsSpan);
        Assert.True(argConv.IsImplicit);
        Assert.False(argConv.IsUserDefined);
        Assert.False(argConv.IsIdentity);
    }

    [Fact]
    public void Conversion_Array_Span_Explicit_SemanticModel()
    {
        var source = """
            using System;
            class C<T, U>
                where T : class
                where U : class, T
            {
                ReadOnlySpan<T> F1(T[] x) => (ReadOnlySpan<T>)x;
                ReadOnlySpan<U> F2(T[] x) => (ReadOnlySpan<U>)x;
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);

        var casts = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().ToImmutableArray();
        Assert.Equal(2, casts.Length);

        {
            var cast = casts[0];
            Assert.Equal("(ReadOnlySpan<T>)x", cast.ToString());

            var op = (IConversionOperation)model.GetOperation(cast)!;
            var conv = op.GetConversion();
            Assert.Equal(ConversionKind.ExplicitSpan, conv.Kind);

            model.VerifyOperationTree(cast, """
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<T>) (Syntax: '(ReadOnlySpan<T>)x')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                """);
        }

        {
            var cast = casts[1];
            Assert.Equal("(ReadOnlySpan<U>)x", cast.ToString());

            var op = (IConversionOperation)model.GetOperation(cast)!;
            var conv = op.GetConversion();
            Assert.Equal(ConversionKind.ExplicitSpan, conv.Kind);

            model.VerifyOperationTree(cast, """
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<U>) (Syntax: '(ReadOnlySpan<U>)x')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                """);
        }
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,41): error CS0029: Cannot implicitly convert type 'int[]' to 'System.Span<string>'
            //     System.Span<string> M(int[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[]", "System.Span<string>").WithLocation(3, 41));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Explicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<string> M(int[] arg) => (Span<string>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,34): error CS0030: Cannot convert type 'int[]' to 'System.Span<string>'
            //     Span<string> M(int[] arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<string>)arg").WithArguments("int[]", "System.Span<string>").WithLocation(4, 34));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                System.ReadOnlySpan<string> M(int[] arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,49): error CS0029: Cannot implicitly convert type 'int[]' to 'System.ReadOnlySpan<string>'
            //     System.ReadOnlySpan<string> M(int[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("int[]", "System.ReadOnlySpan<string>").WithLocation(3, 49));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_Array_Implicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                string[] M(Span<string> arg) => arg;
            }
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator T[](Span<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "string[] System.Span<string>.op_Implicit(System.Span<string>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Explicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(int[] arg) => (ReadOnlySpan<string>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,42): error CS0030: Cannot convert type 'int[]' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M(int[] arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("int[]", "System.ReadOnlySpan<string>").WithLocation(4, 42));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(Span<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,46): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M(Span<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<int>", "System.ReadOnlySpan<string>").WithLocation(4, 46));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Explicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(Span<int> arg) => (ReadOnlySpan<string>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,46): error CS0030: Cannot convert type 'System.Span<int>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M(Span<int> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.Span<int>", "System.ReadOnlySpan<string>").WithLocation(4, 46));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_Struct(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<int> M(Span<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_Struct_Generic(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<S<object>> M(Span<S<string>> arg) => arg;
            }
            struct S<T>;
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,55): error CS0029: Cannot implicitly convert type 'System.Span<S<string>>' to 'System.ReadOnlySpan<S<object>>'
            //     ReadOnlySpan<S<object>> M(Span<S<string>> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<S<string>>", "System.ReadOnlySpan<S<object>>").WithLocation(4, 55));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_Struct_Generic_NullabilityAnnotation(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<S<string?>> M(Span<S<string>> arg) => arg;
            }
            struct S<T>;
            """;
        var targetType = langVersion > LanguageVersion.CSharp13 ? "System.Span<S<string>>" : "System.ReadOnlySpan<S<string>>";
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,56): warning CS8619: Nullability of reference types in value of type 'Span<S<string>>' doesn't match target type 'ReadOnlySpan<S<string?>>'.
            //     ReadOnlySpan<S<string?>> M(Span<S<string>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments(targetType, "System.ReadOnlySpan<S<string?>>").WithLocation(5, 56));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(ReadOnlySpan<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,54): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<int>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M(ReadOnlySpan<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<int>", "System.ReadOnlySpan<string>").WithLocation(4, 54));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Explicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(ReadOnlySpan<int> arg) => (ReadOnlySpan<string>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,54): error CS0030: Cannot convert type 'System.ReadOnlySpan<int>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M(ReadOnlySpan<int> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<int>", "System.ReadOnlySpan<string>").WithLocation(4, 54));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_Struct(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<int> M(ReadOnlySpan<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_Struct_Generic(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<S<object>> M(ReadOnlySpan<S<string>> arg) => arg;
            }
            struct S<T>;
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,63): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<S<string>>' to 'System.ReadOnlySpan<S<object>>'
            //     ReadOnlySpan<S<object>> M(ReadOnlySpan<S<string>> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<S<string>>", "System.ReadOnlySpan<S<object>>").WithLocation(4, 63));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_Struct_Generic_NullabilityAnnotation(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<S<string?>> M(ReadOnlySpan<S<string>> arg) => arg;
            }
            struct S<T>;
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,64): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<S<string>>' doesn't match target type 'ReadOnlySpan<S<string?>>'.
            //     ReadOnlySpan<S<string?>> M(ReadOnlySpan<S<string>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<S<string>>", "System.ReadOnlySpan<S<string?>>").WithLocation(5, 64));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_UserDefined_01(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<string> M(ReadOnlySpan<object> arg) => arg;
            }
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator ReadOnlySpan<string>(ReadOnlySpan<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<object>.op_Implicit(System.ReadOnlySpan<object>)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_UserDefined_02()
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<object> M(ReadOnlySpan<string> arg) => arg;
            }
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator ReadOnlySpan<object>(ReadOnlySpan<T> span) => default;
                    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> s) where TDerived : class, T => throw null;
                }
            }
            """;

        CompileAndVerify(source, parseOptions: TestOptions.Regular13, verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<string>.op_Implicit(System.ReadOnlySpan<string>)"
              IL_0006:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_0006:  ret
            }
            """;

        CompileAndVerify(source, parseOptions: TestOptions.RegularNext, verify: Verification.FailsILVerify)
            .VerifyDiagnostics().VerifyIL("C.M", expectedIl);
        CompileAndVerify(source, verify: Verification.FailsILVerify)
            .VerifyDiagnostics().VerifyIL("C.M", expectedIl);
    }

    [Theory, CombinatorialData]
    public void Conversion_ReadOnlySpan_Span_Implicit(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        [CombinatorialValues("string", "object")] string type)
    {
        var source = $$"""
            using System;
            class C
            {
                Span<{{type}}> M(ReadOnlySpan<string> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,49): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<string>' to 'System.Span<string>'
            //     Span<string> M(ReadOnlySpan<string> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<string>", $"System.Span<{type}>").WithLocation(4, 49));
    }

    [Theory, CombinatorialData]
    public void Conversion_ReadOnlySpan_Span_Explicit(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        [CombinatorialValues("string", "object")] string type)
    {
        var source = $$"""
            using System;
            class C
            {
                Span<{{type}}> M(ReadOnlySpan<string> arg) => (Span<{{type}}>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,49): error CS0030: Cannot convert type 'System.ReadOnlySpan<string>' to 'System.Span<string>'
            //     Span<string> M(ReadOnlySpan<string> arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, $"(Span<{type}>)arg").WithArguments("System.ReadOnlySpan<string>", $"System.Span<{type}>").WithLocation(4, 49));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_Span_Implicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<string> M(ReadOnlySpan<string> arg) => arg;
            }
            namespace System
            {
                public readonly ref struct Span<T>;
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator Span<T>(ReadOnlySpan<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.Span<string> System.ReadOnlySpan<string>.op_Implicit(System.ReadOnlySpan<string>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_Span_Explicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<string> M(ReadOnlySpan<string> arg) => (Span<string>)arg;
            }
            namespace System
            {
                public readonly ref struct Span<T>;
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static explicit operator Span<T>(ReadOnlySpan<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.Span<string> System.ReadOnlySpan<string>.op_Explicit(System.ReadOnlySpan<string>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_Span_Explicit_UserDefined_Covariant(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<string> M(ReadOnlySpan<object> arg) => (Span<string>)arg;
            }
            namespace System
            {
                public readonly ref struct Span<T>;
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static explicit operator Span<string>(ReadOnlySpan<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.Span<string> System.ReadOnlySpan<object>.op_Explicit(System.ReadOnlySpan<object>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Implicit_UnrelatedElementType(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ReadOnlySpan<short> M(string arg) => arg;
            }

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,42): error CS0029: Cannot implicitly convert type 'string' to 'System.ReadOnlySpan<short>'
            //     ReadOnlySpan<short> M(string arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("string", "System.ReadOnlySpan<short>").WithLocation(4, 42));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_Span_Implicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<char> M(string arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,33): error CS0029: Cannot implicitly convert type 'string' to 'System.Span<char>'
            //     Span<char> M(string arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("string", "System.Span<char>").WithLocation(4, 33));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_Span_Explicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<char> M(string arg) => (Span<char>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,33): error CS0030: Cannot convert type 'string' to 'System.Span<char>'
            //     Span<char> M(string arg) => (Span<char>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<char>)arg").WithArguments("string", "System.Span<char>").WithLocation(4, 33));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_Span_Implicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                Span<char> M(string arg) => arg;
            }
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator Span<T>(string s) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify)
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.Span<char> System.Span<char>.op_Implicit(string)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_string_Implicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                string M(ReadOnlySpan<char> arg) => arg;
            }
            namespace System
            {
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator string(ReadOnlySpan<T> span) => default;
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion))
            .VerifyDiagnostics()
            .VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "string System.ReadOnlySpan<char>.op_Implicit(System.ReadOnlySpan<char>)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_NullableAnalysis()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                Span<string> M1(string[] arg) => arg;
                Span<string> M2(string?[] arg) => arg;
                Span<string?> M3(string[] arg) => arg;
                Span<string?> M4(string?[] arg) => arg;

                Span<string> M5(string?[] arg) => (Span<string>)arg;
                Span<string> M6(string?[] arg) => (Span<string?>)arg;
                Span<string?> M7(string[] arg) => (Span<string>)arg;
                Span<string?> M8(string[] arg) => (Span<string?>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,39): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     Span<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "string[]").WithLocation(6, 39),
            // (10,39): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     Span<string> M5(string?[] arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string>)arg").WithArguments("string?[]", "string[]").WithLocation(10, 39),
            // (11,39): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'Span<string>'.
            //     Span<string> M6(string?[] arg) => (Span<string?>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string?>)arg").WithArguments("System.Span<string?>", "System.Span<string>").WithLocation(11, 39),
            // (12,39): warning CS8619: Nullability of reference types in value of type 'Span<string>' doesn't match target type 'Span<string?>'.
            //     Span<string?> M7(string[] arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string>)arg").WithArguments("System.Span<string>", "System.Span<string?>").WithLocation(12, 39));

        var expectedDiagnostics = new[]
        {
            // (6,39): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'Span<string>'.
            //     Span<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "System.Span<string>").WithLocation(6, 39),
            // (7,39): warning CS8619: Nullability of reference types in value of type 'string[]' doesn't match target type 'Span<string?>'.
            //     Span<string?> M3(string[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string[]", "System.Span<string?>").WithLocation(7, 39),
            // (10,39): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'Span<string>'.
            //     Span<string> M5(string?[] arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string>)arg").WithArguments("string?[]", "System.Span<string>").WithLocation(10, 39),
            // (11,39): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'Span<string>'.
            //     Span<string> M6(string?[] arg) => (Span<string?>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string?>)arg").WithArguments("System.Span<string?>", "System.Span<string>").WithLocation(11, 39),
            // (12,39): warning CS8619: Nullability of reference types in value of type 'Span<string>' doesn't match target type 'Span<string?>'.
            //     Span<string?> M7(string[] arg) => (Span<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string>)arg").WithArguments("System.Span<string>", "System.Span<string?>").WithLocation(12, 39),
            // (13,39): warning CS8619: Nullability of reference types in value of type 'string[]' doesn't match target type 'Span<string?>'.
            //     Span<string?> M8(string[] arg) => (Span<string?>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string?>)arg").WithArguments("string[]", "System.Span<string?>").WithLocation(13, 39)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<string> M1(string[] arg) => arg;
                ReadOnlySpan<string> M2(string?[] arg) => arg;
                ReadOnlySpan<string?> M3(string[] arg) => arg;
                ReadOnlySpan<string?> M4(string?[] arg) => arg;
                ReadOnlySpan<object> M5(string?[] arg) => arg;

                ReadOnlySpan<string> M6(string?[] arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<string> M7(object?[] arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<object> M8(string?[] arg) => (ReadOnlySpan<object>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     ReadOnlySpan<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "string[]").WithLocation(6, 47),
            // (9,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'object[]'.
            //     ReadOnlySpan<object> M5(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "object[]").WithLocation(9, 47),
            // (11,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'string[]'.
            //     ReadOnlySpan<string> M6(string?[] arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("string?[]", "string[]").WithLocation(11, 47),
            // (12,47): warning CS8619: Nullability of reference types in value of type 'object?[]' doesn't match target type 'string[]'.
            //     ReadOnlySpan<string> M7(object?[] arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("object?[]", "string[]").WithLocation(12, 47),
            // (13,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'object[]'.
            //     ReadOnlySpan<object> M8(string?[] arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<object>)arg").WithArguments("string?[]", "object[]").WithLocation(13, 47));

        var expectedDiagnostics = new[]
        {
            // (6,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M2(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "System.ReadOnlySpan<string>").WithLocation(6, 47),
            // (9,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M5(string?[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[]", "System.ReadOnlySpan<object>").WithLocation(9, 47),
            // (11,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M6(string?[] arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("string?[]", "System.ReadOnlySpan<string>").WithLocation(11, 47),
            // (12,47): warning CS8619: Nullability of reference types in value of type 'object?[]' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M7(object?[] arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("object?[]", "System.ReadOnlySpan<string>").WithLocation(12, 47),
            // (13,47): warning CS8619: Nullability of reference types in value of type 'string?[]' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M8(string?[] arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<object>)arg").WithArguments("string?[]", "System.ReadOnlySpan<object>").WithLocation(13, 47)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_NullableAnalysis()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<string> M1(Span<string> arg) => arg;
                ReadOnlySpan<string> M2(Span<string?> arg) => arg;
                ReadOnlySpan<string?> M3(Span<string> arg) => arg;
                ReadOnlySpan<string?> M4(Span<string?> arg) => arg;
                ReadOnlySpan<object> M5(Span<string?> arg) => arg;

                ReadOnlySpan<string> M6(Span<string?> arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<string> M7(Span<object?> arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<object> M8(Span<string?> arg) => (ReadOnlySpan<object>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,51): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M2(Span<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(6, 51),
            // (7,51): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string>' doesn't match target type 'ReadOnlySpan<string?>'.
            //     ReadOnlySpan<string?> M3(Span<string> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string>", "System.ReadOnlySpan<string?>").WithLocation(7, 51),
            // (9,51): error CS0029: Cannot implicitly convert type 'System.Span<string?>' to 'System.ReadOnlySpan<object>'
            //     ReadOnlySpan<object> M5(Span<string?> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<object>").WithLocation(9, 51),
            // (11,51): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M6(Span<string?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(11, 51),
            // (12,51): error CS0030: Cannot convert type 'System.Span<object?>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M7(Span<object?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.Span<object?>", "System.ReadOnlySpan<string>").WithLocation(12, 51),
            // (13,51): error CS0030: Cannot convert type 'System.Span<string?>' to 'System.ReadOnlySpan<object>'
            //     ReadOnlySpan<object> M8(Span<string?> arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<object>)arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<object>").WithLocation(13, 51));

        var expectedDiagnostics = new[]
        {
            // (6,51): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M2(Span<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<string>").WithLocation(6, 51),
            // (9,51): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M5(Span<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<object>").WithLocation(9, 51),
            // (11,51): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M6(Span<string?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<string>").WithLocation(11, 51),
            // (12,51): error CS0030: Cannot convert type 'System.Span<object?>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M7(Span<object?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.Span<object?>", "System.ReadOnlySpan<string>").WithLocation(12, 51),
            // (13,51): warning CS8619: Nullability of reference types in value of type 'Span<string?>' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M8(Span<string?> arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<object>)arg").WithArguments("System.Span<string?>", "System.ReadOnlySpan<object>").WithLocation(13, 51)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_NullableAnalysis()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<string> M1(ReadOnlySpan<string> arg) => arg;
                ReadOnlySpan<string> M2(ReadOnlySpan<string?> arg) => arg;
                ReadOnlySpan<string?> M3(ReadOnlySpan<string> arg) => arg;
                ReadOnlySpan<string?> M4(ReadOnlySpan<string?> arg) => arg;
                ReadOnlySpan<object> M5(ReadOnlySpan<string?> arg) => arg;

                ReadOnlySpan<string> M6(ReadOnlySpan<string?> arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<string> M7(ReadOnlySpan<object?> arg) => (ReadOnlySpan<string>)arg;
                ReadOnlySpan<object> M8(ReadOnlySpan<string?> arg) => (ReadOnlySpan<object>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M2(ReadOnlySpan<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(6, 59),
            // (7,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string>' doesn't match target type 'ReadOnlySpan<string?>'.
            //     ReadOnlySpan<string?> M3(ReadOnlySpan<string> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string>", "System.ReadOnlySpan<string?>").WithLocation(7, 59),
            // (9,59): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<string?>' to 'System.ReadOnlySpan<object>'
            //     ReadOnlySpan<object> M5(ReadOnlySpan<string?> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<object>").WithLocation(9, 59),
            // (11,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M6(ReadOnlySpan<string?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(11, 59),
            // (12,59): error CS0030: Cannot convert type 'System.ReadOnlySpan<object?>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M7(ReadOnlySpan<object?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<object?>", "System.ReadOnlySpan<string>").WithLocation(12, 59),
            // (13,59): error CS0030: Cannot convert type 'System.ReadOnlySpan<string?>' to 'System.ReadOnlySpan<object>'
            //     ReadOnlySpan<object> M8(ReadOnlySpan<string?> arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<object>)arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<object>").WithLocation(13, 59));

        var expectedDiagnostics = new[]
        {
            // (6,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M2(ReadOnlySpan<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(6, 59),
            // (9,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M5(ReadOnlySpan<string?> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<object>").WithLocation(9, 59),
            // (11,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<string>'.
            //     ReadOnlySpan<string> M6(ReadOnlySpan<string?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<string>").WithLocation(11, 59),
            // (12,59): error CS0030: Cannot convert type 'System.ReadOnlySpan<object?>' to 'System.ReadOnlySpan<string>'
            //     ReadOnlySpan<string> M7(ReadOnlySpan<object?> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<object?>", "System.ReadOnlySpan<string>").WithLocation(12, 59),
            // (13,59): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<string?>' doesn't match target type 'ReadOnlySpan<object>'.
            //     ReadOnlySpan<object> M8(ReadOnlySpan<string?> arg) => (ReadOnlySpan<object>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<object>)arg").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<object>").WithLocation(13, 59)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Implicit_NullableAnalysis(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<char> M1(string arg) => arg;
                ReadOnlySpan<char> M2(string? arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_NestedArrays()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                Span<string[]> M1(string[][] arg) => arg;
                Span<string[]> M2(string?[][] arg) => arg;
                Span<string?[]> M3(string[][] arg) => arg;
                Span<string?[]> M4(string?[][] arg) => arg;

                Span<string[]> M5(string?[][] arg) => (Span<string[]>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,43): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     Span<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "string[][]").WithLocation(6, 43),
            // (10,43): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     Span<string[]> M5(string?[][] arg) => (Span<string[]>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string[]>)arg").WithArguments("string?[][]", "string[][]").WithLocation(10, 43));

        var expectedDiagnostics = new[]
        {
            // (6,43): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'Span<string[]>'.
            //     Span<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "System.Span<string[]>").WithLocation(6, 43),
            // (7,43): warning CS8619: Nullability of reference types in value of type 'string[][]' doesn't match target type 'Span<string?[]>'.
            //     Span<string?[]> M3(string[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string[][]", "System.Span<string?[]>").WithLocation(7, 43),
            // (10,43): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'Span<string[]>'.
            //     Span<string[]> M5(string?[][] arg) => (Span<string[]>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<string[]>)arg").WithArguments("string?[][]", "System.Span<string[]>").WithLocation(10, 43)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis_NestedArrays()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<string[]> M1(string[][] arg) => arg;
                ReadOnlySpan<string[]> M2(string?[][] arg) => arg;
                ReadOnlySpan<string?[]> M3(string[][] arg) => arg;
                ReadOnlySpan<string?[]> M4(string?[][] arg) => arg;
                ReadOnlySpan<object[]> M5(string?[][] arg) => arg;

                ReadOnlySpan<string[]> M6(string?[][] arg) => (ReadOnlySpan<string[]>)arg;
                ReadOnlySpan<string[]> M7(object?[][] arg) => (ReadOnlySpan<string[]>)arg;
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     ReadOnlySpan<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "string[][]").WithLocation(6, 51),
            // (9,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'object[][]'.
            //     ReadOnlySpan<object[]> M5(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "object[][]").WithLocation(9, 51),
            // (11,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'string[][]'.
            //     ReadOnlySpan<string[]> M6(string?[][] arg) => (ReadOnlySpan<string[]>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string[]>)arg").WithArguments("string?[][]", "string[][]").WithLocation(11, 51),
            // (12,51): warning CS8619: Nullability of reference types in value of type 'object?[][]' doesn't match target type 'string[][]'.
            //     ReadOnlySpan<string[]> M7(object?[][] arg) => (ReadOnlySpan<string[]>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string[]>)arg").WithArguments("object?[][]", "string[][]").WithLocation(12, 51));

        var expectedDiagnostics = new[]
        {
            // (6,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'ReadOnlySpan<string[]>'.
            //     ReadOnlySpan<string[]> M2(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "System.ReadOnlySpan<string[]>").WithLocation(6, 51),
            // (9,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'ReadOnlySpan<object[]>'.
            //     ReadOnlySpan<object[]> M5(string?[][] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("string?[][]", "System.ReadOnlySpan<object[]>").WithLocation(9, 51),
            // (11,51): warning CS8619: Nullability of reference types in value of type 'string?[][]' doesn't match target type 'ReadOnlySpan<string[]>'.
            //     ReadOnlySpan<string[]> M6(string?[][] arg) => (ReadOnlySpan<string[]>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<string[]>)arg").WithArguments("string?[][]", "System.ReadOnlySpan<string[]>").WithLocation(11, 51)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_NestedNullability(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                Span<S<string>> M1(S<string>[] arg) => arg;
                Span<S<string>> M2(S<string?>[] arg) => arg;
                Span<S<string?>> M3(S<string>[] arg) => arg;
                Span<S<string?>> M4(S<string?>[] arg) => arg;

                Span<S<string>> M5(S<string?>[] arg) => (Span<S<string>>)arg;
            }
            struct S<T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (6,45): warning CS8619: Nullability of reference types in value of type 'S<string?>[]' doesn't match target type 'S<string>[]'.
            //     Span<S<string>> M2(S<string?>[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("S<string?>[]", targetType("string")).WithLocation(6, 45),
            // (7,45): warning CS8619: Nullability of reference types in value of type 'S<string>[]' doesn't match target type 'S<string?>[]'.
            //     Span<S<string?>> M3(S<string>[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("S<string>[]", targetType("string?")).WithLocation(7, 45),
            // (10,45): warning CS8619: Nullability of reference types in value of type 'S<string?>[]' doesn't match target type 'S<string>[]'.
            //     Span<S<string>> M5(S<string?>[] arg) => (Span<S<string>>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(Span<S<string>>)arg").WithArguments("S<string?>[]", targetType("string")).WithLocation(10, 45));

        string targetType(string inner)
            => langVersion > LanguageVersion.CSharp13 ? $"System.Span<S<{inner}>>" : $"S<{inner}>[]";
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis_NestedNullability(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<S<string>> M1(S<string>[] arg) => arg;
                ReadOnlySpan<S<string>> M2(S<string?>[] arg) => arg;
                ReadOnlySpan<S<string?>> M3(S<string>[] arg) => arg;
                ReadOnlySpan<S<string?>> M4(S<string?>[] arg) => arg;
                ReadOnlySpan<S<object>> M5(S<string?>[] arg) => arg;

                ReadOnlySpan<S<string>> M6(S<string?>[] arg) => (ReadOnlySpan<S<string>>)arg;
                ReadOnlySpan<S<string>> M7(S<object?>[] arg) => (ReadOnlySpan<S<string>>)arg;
            }
            struct S<T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (6,53): warning CS8619: Nullability of reference types in value of type 'S<string?>[]' doesn't match target type 'S<string>[]'.
            //     ReadOnlySpan<S<string>> M2(S<string?>[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("S<string?>[]", targetType("string")).WithLocation(6, 53),
            // (7,53): warning CS8619: Nullability of reference types in value of type 'S<string>[]' doesn't match target type 'S<string?>[]'.
            //     ReadOnlySpan<S<string?>> M3(S<string>[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("S<string>[]", targetType("string?")).WithLocation(7, 53),
            // (9,53): error CS0029: Cannot implicitly convert type 'S<string?>[]' to 'System.ReadOnlySpan<S<object>>'
            //     ReadOnlySpan<S<object>> M5(S<string?>[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("S<string?>[]", "System.ReadOnlySpan<S<object>>").WithLocation(9, 53),
            // (11,53): warning CS8619: Nullability of reference types in value of type 'S<string?>[]' doesn't match target type 'S<string>[]'.
            //     ReadOnlySpan<S<string>> M6(S<string?>[] arg) => (ReadOnlySpan<S<string>>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<S<string>>)arg").WithArguments("S<string?>[]", targetType("string")).WithLocation(11, 53),
            // (12,53): error CS0030: Cannot convert type 'S<object?>[]' to 'System.ReadOnlySpan<S<string>>'
            //     ReadOnlySpan<S<string>> M7(S<object?>[] arg) => (ReadOnlySpan<S<string>>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<S<string>>)arg").WithArguments("S<object?>[]", "System.ReadOnlySpan<S<string>>").WithLocation(12, 53));

        string targetType(string inner)
            => langVersion > LanguageVersion.CSharp13 ? $"System.ReadOnlySpan<S<{inner}>>" : $"S<{inner}>[]";
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_NullableAnalysis_NestedNullability()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<I<string>> M1(Span<I<string>> arg) => arg;
                ReadOnlySpan<I<string>> M2(Span<I<string?>> arg) => arg;
                ReadOnlySpan<I<string?>> M3(Span<I<string>> arg) => arg;
                ReadOnlySpan<I<string?>> M4(Span<I<string?>> arg) => arg;
                ReadOnlySpan<I<object>> M5(Span<I<string?>> arg) => arg;

                ReadOnlySpan<I<string>> M6(Span<I<string?>> arg) => (ReadOnlySpan<I<string>>)arg;
                ReadOnlySpan<I<string>> M7(Span<I<object?>> arg) => (ReadOnlySpan<I<string>>)arg;
            }
            interface I<out T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,57): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<I<string?>>' doesn't match target type 'ReadOnlySpan<I<string>>'.
            //     ReadOnlySpan<I<string>> M2(Span<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<I<string?>>", "System.ReadOnlySpan<I<string>>").WithLocation(6, 57),
            // (7,57): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<I<string>>' doesn't match target type 'ReadOnlySpan<I<string?>>'.
            //     ReadOnlySpan<I<string?>> M3(Span<I<string>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<I<string>>", "System.ReadOnlySpan<I<string?>>").WithLocation(7, 57),
            // (9,57): error CS0029: Cannot implicitly convert type 'System.Span<I<string?>>' to 'System.ReadOnlySpan<I<object>>'
            //     ReadOnlySpan<I<object>> M5(Span<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<I<string?>>", "System.ReadOnlySpan<I<object>>").WithLocation(9, 57),
            // (11,57): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<I<string?>>' doesn't match target type 'ReadOnlySpan<I<string>>'.
            //     ReadOnlySpan<I<string>> M6(Span<I<string?>> arg) => (ReadOnlySpan<I<string>>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<I<string>>)arg").WithArguments("System.ReadOnlySpan<I<string?>>", "System.ReadOnlySpan<I<string>>").WithLocation(11, 57),
            // (12,57): error CS0030: Cannot convert type 'System.Span<I<object?>>' to 'System.ReadOnlySpan<I<string>>'
            //     ReadOnlySpan<I<string>> M7(Span<I<object?>> arg) => (ReadOnlySpan<I<string>>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<I<string>>)arg").WithArguments("System.Span<I<object?>>", "System.ReadOnlySpan<I<string>>").WithLocation(12, 57));

        var expectedDiagnostics = new[]
        {
            // (6,57): warning CS8619: Nullability of reference types in value of type 'Span<I<string?>>' doesn't match target type 'ReadOnlySpan<I<string>>'.
            //     ReadOnlySpan<I<string>> M2(Span<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.Span<I<string?>>", "System.ReadOnlySpan<I<string>>").WithLocation(6, 57),
            // (9,57): warning CS8619: Nullability of reference types in value of type 'Span<I<string?>>' doesn't match target type 'ReadOnlySpan<I<object>>'.
            //     ReadOnlySpan<I<object>> M5(Span<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.Span<I<string?>>", "System.ReadOnlySpan<I<object>>").WithLocation(9, 57),
            // (11,57): warning CS8619: Nullability of reference types in value of type 'Span<I<string?>>' doesn't match target type 'ReadOnlySpan<I<string>>'.
            //     ReadOnlySpan<I<string>> M6(Span<I<string?>> arg) => (ReadOnlySpan<I<string>>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<I<string>>)arg").WithArguments("System.Span<I<string?>>", "System.ReadOnlySpan<I<string>>").WithLocation(11, 57),
            // (12,57): error CS0030: Cannot convert type 'System.Span<I<object?>>' to 'System.ReadOnlySpan<I<string>>'
            //     ReadOnlySpan<I<string>> M7(Span<I<object?>> arg) => (ReadOnlySpan<I<string>>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<I<string>>)arg").WithArguments("System.Span<I<object?>>", "System.ReadOnlySpan<I<string>>").WithLocation(12, 57)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_NullableAnalysis_NestedNullability(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<S<string>> M1(ReadOnlySpan<S<string>> arg) => arg;
                ReadOnlySpan<S<string>> M2(ReadOnlySpan<S<string?>> arg) => arg;
                ReadOnlySpan<S<string?>> M3(ReadOnlySpan<S<string>> arg) => arg;
                ReadOnlySpan<S<string?>> M4(ReadOnlySpan<S<string?>> arg) => arg;
                ReadOnlySpan<S<object>> M5(ReadOnlySpan<S<string?>> arg) => arg;

                ReadOnlySpan<S<string>> M6(ReadOnlySpan<S<string?>> arg) => (ReadOnlySpan<S<string>>)arg;
                ReadOnlySpan<S<string>> M7(ReadOnlySpan<S<object?>> arg) => (ReadOnlySpan<S<string>>)arg;
            }
            struct S<T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,65): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<S<string?>>' doesn't match target type 'ReadOnlySpan<S<string>>'.
            //     ReadOnlySpan<S<string>> M2(ReadOnlySpan<S<string?>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<S<string?>>", "System.ReadOnlySpan<S<string>>").WithLocation(6, 65),
            // (7,65): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<S<string>>' doesn't match target type 'ReadOnlySpan<S<string?>>'.
            //     ReadOnlySpan<S<string?>> M3(ReadOnlySpan<S<string>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<S<string>>", "System.ReadOnlySpan<S<string?>>").WithLocation(7, 65),
            // (9,65): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<S<string?>>' to 'System.ReadOnlySpan<S<object>>'
            //     ReadOnlySpan<S<object>> M5(ReadOnlySpan<S<string?>> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<S<string?>>", "System.ReadOnlySpan<S<object>>").WithLocation(9, 65),
            // (11,65): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<S<string?>>' doesn't match target type 'ReadOnlySpan<S<string>>'.
            //     ReadOnlySpan<S<string>> M6(ReadOnlySpan<S<string?>> arg) => (ReadOnlySpan<S<string>>)arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(ReadOnlySpan<S<string>>)arg").WithArguments("System.ReadOnlySpan<S<string?>>", "System.ReadOnlySpan<S<string>>").WithLocation(11, 65),
            // (12,65): error CS0030: Cannot convert type 'System.ReadOnlySpan<S<object?>>' to 'System.ReadOnlySpan<S<string>>'
            //     ReadOnlySpan<S<string>> M7(ReadOnlySpan<S<object?>> arg) => (ReadOnlySpan<S<string>>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<S<string>>)arg").WithArguments("System.ReadOnlySpan<S<object?>>", "System.ReadOnlySpan<S<string>>").WithLocation(12, 65));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_NullableAnalysis_NestedNullability_Covariant(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<I<object>> M(I<string?>[] arg) => arg;
            }
            interface I<out T> { }
            """;
        var targetType = langVersion > LanguageVersion.CSharp13 ? "System.ReadOnlySpan<I<object>>" : "I<object>[]";
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,52): warning CS8619: Nullability of reference types in value of type 'I<string?>[]' doesn't match target type 'I<object>[]'.
            //     ReadOnlySpan<I<object>> M(I<string?>[] arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("I<string?>[]", targetType).WithLocation(5, 52));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_NullableAnalysis_NestedNullability_Covariant()
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                ReadOnlySpan<I<object>> M(ReadOnlySpan<I<string?>> arg) => arg;
            }
            interface I<out T> { }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (5,64): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<I<string?>>' to 'System.ReadOnlySpan<I<object>>'
            //     ReadOnlySpan<I<object>> M(ReadOnlySpan<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<I<string?>>", "System.ReadOnlySpan<I<object>>").WithLocation(5, 64));

        var expectedDiagnostics = new[]
        {
            // (5,64): warning CS8619: Nullability of reference types in value of type 'ReadOnlySpan<I<string?>>' doesn't match target type 'ReadOnlySpan<I<object>>'.
            //     ReadOnlySpan<I<object>> M(ReadOnlySpan<I<string?>> arg) => arg;
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "arg").WithArguments("System.ReadOnlySpan<I<string?>>", "System.ReadOnlySpan<I<object>>").WithLocation(5, 64)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_Outer(LanguageVersion langVersion)
    {
        var source = """
            #nullable enable
            using System;
            class C
            {
                Span<string>? M1(string[] arg) => arg;
                ReadOnlySpan<string>? M2(string[] arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,19): error CS9244: The type 'Span<string>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
            //     Span<string>? M1(string[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M1").WithArguments("System.Nullable<T>", "T", "System.Span<string>").WithLocation(5, 19),
            // (6,27): error CS9244: The type 'ReadOnlySpan<string>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
            //     ReadOnlySpan<string>? M2(string[] arg) => arg;
            Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M2").WithArguments("System.Nullable<T>", "T", "System.ReadOnlySpan<string>").WithLocation(6, 27));
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_NullableAnalysis_ExtensionMethodReceiver()
    {
        var source = """
            #nullable enable
            using System;
            static class C
            {
                static void MS(string[] a, string?[] b)
                {
                    a.M1(); b.M1();
                    a.M2(); b.M2();
                    a.M3(); b.M3();
                    a.M4(); b.M4();
                }
                static void M1(this Span<string> arg) { }
                static void M2(this Span<string?> arg) { }
                static void M3(this ReadOnlySpan<string> arg) { }
                static void M4(this ReadOnlySpan<string?> arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (7,17): warning CS8620: Argument of type 'string?[]' cannot be used for parameter 'arg' of type 'Span<string>' in 'void C.M1(Span<string> arg)' due to differences in the nullability of reference types.
            //         a.M1(); b.M1();
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "b").WithArguments("string?[]", "System.Span<string>", "arg", "void C.M1(Span<string> arg)").WithLocation(7, 17),
            // (8,9): warning CS8620: Argument of type 'string[]' cannot be used for parameter 'arg' of type 'Span<string?>' in 'void C.M2(Span<string?> arg)' due to differences in the nullability of reference types.
            //         a.M2(); b.M2();
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "a").WithArguments("string[]", "System.Span<string?>", "arg", "void C.M2(Span<string?> arg)").WithLocation(8, 9),
            // (9,17): warning CS8620: Argument of type 'string?[]' cannot be used for parameter 'arg' of type 'ReadOnlySpan<string>' in 'void C.M3(ReadOnlySpan<string> arg)' due to differences in the nullability of reference types.
            //         a.M3(); b.M3();
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "b").WithArguments("string?[]", "System.ReadOnlySpan<string>", "arg", "void C.M3(ReadOnlySpan<string> arg)").WithLocation(9, 17));
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Implicit_NullableAnalysis_ExtensionMethodReceiver()
    {
        var source = """
            #nullable enable
            using System;
            static class C
            {
                static void MS(Span<string> a, Span<string?> b)
                {
                    a.M1(); b.M1();
                    a.M2(); b.M2();
                }
                static void M1(this ReadOnlySpan<string> arg) { }
                static void M2(this ReadOnlySpan<string?> arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (7,17): warning CS8620: Argument of type 'Span<string?>' cannot be used for parameter 'arg' of type 'ReadOnlySpan<string>' in 'void C.M1(ReadOnlySpan<string> arg)' due to differences in the nullability of reference types.
            //         a.M1(); b.M1();
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "b").WithArguments("System.Span<string?>", "System.ReadOnlySpan<string>", "arg", "void C.M1(ReadOnlySpan<string> arg)").WithLocation(7, 17));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_NullableAnalysis_ExtensionMethodReceiver()
    {
        var source = """
            #nullable enable
            using System;
            static class C
            {
                static void MS(ReadOnlySpan<string> a, ReadOnlySpan<string?> b)
                {
                    a.M1(); b.M1();
                    a.M2(); b.M2();
                }
                static void M1(this ReadOnlySpan<object> arg) { }
                static void M2(this ReadOnlySpan<object?> arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (7,17): warning CS8620: Argument of type 'ReadOnlySpan<string?>' cannot be used for parameter 'arg' of type 'ReadOnlySpan<object>' in 'void C.M1(ReadOnlySpan<object> arg)' due to differences in the nullability of reference types.
            //         a.M1(); b.M1();
            Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "b").WithArguments("System.ReadOnlySpan<string?>", "System.ReadOnlySpan<object>", "arg", "void C.M1(ReadOnlySpan<object> arg)").WithLocation(7, 17));
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_Implicit_NullableAnalysis_ExtensionMethodReceiver()
    {
        var source = """
            #nullable enable
            using System;
            static class C
            {
                static void MS(string a, string? b)
                {
                    a.M();
                    b.M();
                }
                static void M(this ReadOnlySpan<char> arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics();
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
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C.M1", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  ldind.ref
              IL_0002:  call       "System.Span<string> System.Span<string>.op_Implicit(string[])"
              IL_0007:  ret
            }
            """);
        verifier.VerifyIL("C.M2", """
            {
              // Code size        8 (0x8)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  ldind.ref
              IL_0002:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
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
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
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
              IL_0006:  call       "System.Span<string> System.Span<string>.op_Implicit(string[])"
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
              IL_0006:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
              IL_000b:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_RefReturn(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                ref Span<string> M1(ref string[] arg) => ref arg;
                ref ReadOnlySpan<string> M2(ref string[] arg) => ref arg;
                ref ReadOnlySpan<object> M3(ref string[] arg) => ref arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,50): error CS8151: The return expression must be of type 'Span<string>' because this method returns by reference
            //     ref Span<string> M1(ref string[] arg) => ref arg;
            Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "arg").WithArguments("System.Span<string>").WithLocation(4, 50),
            // (5,58): error CS8151: The return expression must be of type 'ReadOnlySpan<string>' because this method returns by reference
            //     ref ReadOnlySpan<string> M2(ref string[] arg) => ref arg;
            Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "arg").WithArguments("System.ReadOnlySpan<string>").WithLocation(5, 58),
            // (6,58): error CS8151: The return expression must be of type 'ReadOnlySpan<object>' because this method returns by reference
            //     ref ReadOnlySpan<object> M3(ref string[] arg) => ref arg;
            Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "arg").WithArguments("System.ReadOnlySpan<object>").WithLocation(6, 58));
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
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, TestOptions.DebugDll);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       26 (0x1a)
              .maxstack  1
              .locals init (System.Span<string> V_0, //s
                            System.ReadOnlySpan<string> V_1) //r
              IL_0000:  nop
              IL_0001:  ldarg.0
              IL_0002:  call       "string[] C.A()"
              IL_0007:  call       "System.Span<string> System.Span<string>.op_Implicit(string[])"
              IL_000c:  stloc.0
              IL_000d:  ldarg.0
              IL_000e:  call       "string[] C.A()"
              IL_0013:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
              IL_0018:  stloc.1
              IL_0019:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                 int[] M1(System.Span<int> arg) => arg;
                 int[] M2(System.ReadOnlySpan<int> arg) => arg;
                 object[] M3(System.ReadOnlySpan<string> arg) => arg;
                 string[] M4(System.ReadOnlySpan<object> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,40): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'int[]'
            //      int[] M1(System.Span<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 40),
            // (4,48): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<int>' to 'int[]'
            //      int[] M2(System.ReadOnlySpan<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<int>", "int[]").WithLocation(4, 48),
            // (5,54): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<string>' to 'object[]'
            //      object[] M3(System.ReadOnlySpan<string> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<string>", "object[]").WithLocation(5, 54),
            // (6,54): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<object>' to 'string[]'
            //      string[] M4(System.ReadOnlySpan<object> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<object>", "string[]").WithLocation(6, 54));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 Span<int> M1(ReadOnlySpan<int> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,45): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<int>' to 'System.Span<int>'
            //      Span<int> M1(ReadOnlySpan<int> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(4, 45));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 ReadOnlySpan<string> M1(ReadOnlySpan<object> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,59): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<object>' to 'System.ReadOnlySpan<string>'
            //      ReadOnlySpan<string> M1(ReadOnlySpan<object> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<object>", "System.ReadOnlySpan<string>").WithLocation(4, 59));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 string M1(ReadOnlySpan<char> arg) => arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,43): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<char>' to 'string'
            //      string M1(ReadOnlySpan<char> arg) => arg;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "arg").WithArguments("System.ReadOnlySpan<char>", "string").WithLocation(4, 43));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            class C
            {
                 int[] M1(System.Span<int> arg) => (int[])arg;
                 int[] M2(System.ReadOnlySpan<int> arg) => (int[])arg;
                 object[] M3(System.ReadOnlySpan<string> arg) => (object[])arg;
                 string[] M4(System.ReadOnlySpan<object> arg) => (string[])arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,40): error CS0030: Cannot convert type 'System.Span<int>' to 'int[]'
            //      int[] M1(System.Span<int> arg) => (int[])arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 40),
            // (4,48): error CS0030: Cannot convert type 'System.ReadOnlySpan<int>' to 'int[]'
            //      int[] M2(System.ReadOnlySpan<int> arg) => (int[])arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.ReadOnlySpan<int>", "int[]").WithLocation(4, 48),
            // (5,54): error CS0030: Cannot convert type 'System.ReadOnlySpan<string>' to 'object[]'
            //      object[] M3(System.ReadOnlySpan<string> arg) => (object[])arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object[])arg").WithArguments("System.ReadOnlySpan<string>", "object[]").WithLocation(5, 54),
            // (6,54): error CS0030: Cannot convert type 'System.ReadOnlySpan<object>' to 'string[]'
            //      string[] M4(System.ReadOnlySpan<object> arg) => (string[])arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(string[])arg").WithArguments("System.ReadOnlySpan<object>", "string[]").WithLocation(6, 54));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 Span<int> M1(ReadOnlySpan<int> arg) => (Span<int>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,45): error CS0030: Cannot convert type 'System.ReadOnlySpan<int>' to 'System.Span<int>'
            //      Span<int> M1(ReadOnlySpan<int> arg) => (Span<int>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<int>)arg").WithArguments("System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(4, 45));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 ReadOnlySpan<string> M1(ReadOnlySpan<object> arg) => (ReadOnlySpan<string>)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,59): error CS0030: Cannot convert type 'System.ReadOnlySpan<object>' to 'System.ReadOnlySpan<string>'
            //      ReadOnlySpan<string> M1(ReadOnlySpan<object> arg) => (ReadOnlySpan<string>)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<string>)arg").WithArguments("System.ReadOnlySpan<object>", "System.ReadOnlySpan<string>").WithLocation(4, 59));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 string M1(ReadOnlySpan<char> arg) => (string)arg;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,43): error CS0030: Cannot convert type 'System.ReadOnlySpan<char>' to 'string'
            //      string M1(ReadOnlySpan<char> arg) => (string)arg;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(string)arg").WithArguments("System.ReadOnlySpan<char>", "string").WithLocation(4, 43));
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

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Opposite_Explicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 Span<int> M(ReadOnlySpan<int> arg) => (Span<int>)arg;
            }

            namespace System
            {
                readonly ref struct Span<T>;
                readonly ref struct ReadOnlySpan<T>
                {
                    public static explicit operator Span<T>(ReadOnlySpan<T> span) => throw null;
                }
            }
            """;
        var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion), verify: Verification.FailsILVerify);
        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  call       "System.Span<int> System.ReadOnlySpan<int>.op_Explicit(System.ReadOnlySpan<int>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Opposite_Explicit_UserDefined(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                 string M(ReadOnlySpan<char> arg) => (string)arg;
            }

            namespace System
            {
                readonly ref struct ReadOnlySpan<T>
                {
                    public static explicit operator string(ReadOnlySpan<T> span) => throw null;
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
              IL_0001:  call       "string System.ReadOnlySpan<char>.op_Explicit(System.ReadOnlySpan<char>)"
              IL_0006:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_Params(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C
            {
                void M(string[] a)
                {
                    M1(a);
                    M2(a);
                    M3(a);
                }
                void M1(params Span<string> s) { }
                void M2(params ReadOnlySpan<string> s) { }
                void M3(params string[] s) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       32 (0x20)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.Span<string> System.Span<string>.op_Implicit(string[])"
              IL_0007:  call       "void C.M1(params System.Span<string>)"
              IL_000c:  ldarg.0
              IL_000d:  ldarg.1
              IL_000e:  call       "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
              IL_0013:  call       "void C.M2(params System.ReadOnlySpan<string>)"
              IL_0018:  ldarg.0
              IL_0019:  ldarg.1
              IL_001a:  call       "void C.M3(params string[])"
              IL_001f:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_Implicit_Params_Covariant()
    {
        var source = """
            using System;

            class C
            {
                void M(string[] a)
                {
                    M1(a);
                    M2(a);
                    M3(a);
                }
                void M1(params Span<object> s) { }
                void M2(params ReadOnlySpan<object> s) { }
                void M3(params object[] p) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       38 (0x26)
              .maxstack  2
              .locals init (object[] V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  call       "System.Span<object> System.Span<object>.op_Implicit(object[])"
              IL_0009:  call       "void C.M1(params System.Span<object>)"
              IL_000e:  ldarg.0
              IL_000f:  ldarg.1
              IL_0010:  stloc.0
              IL_0011:  ldloc.0
              IL_0012:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])"
              IL_0017:  call       "void C.M2(params System.ReadOnlySpan<object>)"
              IL_001c:  ldarg.0
              IL_001d:  ldarg.1
              IL_001e:  stloc.0
              IL_001f:  ldloc.0
              IL_0020:  call       "void C.M3(params object[])"
              IL_0025:  ret
            }
            """);

        var expectedIl = """
            {
              // Code size       45 (0x2d)
              .maxstack  5
              .locals init (object[] V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldc.i4.1
              IL_0002:  newarr     "object"
              IL_0007:  dup
              IL_0008:  ldc.i4.0
              IL_0009:  ldarg.1
              IL_000a:  stelem.ref
              IL_000b:  newobj     "System.Span<object>..ctor(object[])"
              IL_0010:  call       "void C.M1(params System.Span<object>)"
              IL_0015:  ldarg.0
              IL_0016:  ldarg.1
              IL_0017:  stloc.0
              IL_0018:  ldloc.0
              IL_0019:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])"
              IL_001e:  call       "void C.M2(params System.ReadOnlySpan<object>)"
              IL_0023:  ldarg.0
              IL_0024:  ldarg.1
              IL_0025:  stloc.0
              IL_0026:  ldloc.0
              IL_0027:  call       "void C.M3(params object[])"
              IL_002c:  ret
            }
            """;

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_Params(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C
            {
                void M(Span<string> a)
                {
                    M1(a);
                }
                void M1(params ReadOnlySpan<string> s) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       13 (0xd)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)"
              IL_0007:  call       "void C.M1(params System.ReadOnlySpan<string>)"
              IL_000c:  ret
            }
            """);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_Params()
    {
        var source = """
            using System;
            class C
            {
                void M(Span<string> a)
                {
                    M1(a);
                }
                void M1(params ReadOnlySpan<object> s) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,12): error CS1503: Argument 1: cannot convert from 'System.Span<string>' to 'object'
            //         M1(a);
            Diagnostic(ErrorCode.ERR_BadArgType, "a").WithArguments("1", "System.Span<string>", "object").WithLocation(6, 12));

        var expectedIl = """
            {
              // Code size       18 (0x12)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)"
              IL_0007:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000c:  call       "void C.M1(params System.ReadOnlySpan<object>)"
              IL_0011:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_CastUp_Implicit_Params()
    {
        var source = """
            using System;
            class C
            {
                void M(ReadOnlySpan<string> a)
                {
                    M1(a);
                }
                void M1(params ReadOnlySpan<object> s) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,12): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<string>' to 'object'
            //         M1(a);
            Diagnostic(ErrorCode.ERR_BadArgType, "a").WithArguments("1", "System.ReadOnlySpan<string>", "object").WithLocation(6, 12));

        var expectedIl = """
            {
              // Code size       13 (0xd)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_0007:  call       "void C.M1(params System.ReadOnlySpan<object>)"
              IL_000c:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_CastUp_Implicit_Params()
    {
        var source = """
            using System;
            class C
            {
                void M(ReadOnlySpan<string> a)
                {
                    M1(a);
                }
                void M1(params ReadOnlySpan<object> s) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,12): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<string>' to 'object'
            //         M1(a);
            Diagnostic(ErrorCode.ERR_BadArgType, "a").WithArguments("1", "System.ReadOnlySpan<string>", "object").WithLocation(6, 12));

        var expectedIl = """
            {
              // Code size       13 (0xd)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_0007:  call       "void C.M1(params System.ReadOnlySpan<object>)"
              IL_000c:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
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

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_MethodGroup_ReturnType(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.R(C.M);
            C.R(() => C.M());

            static class C
            {
                public static int[] M() => new int[] { 1, 2, 3 };
                public static void R(D f) => Console.Write(f()[1]);
            }

            delegate Span<int> D();
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,5): error CS0407: 'int[] C.M()' has the wrong return type
            // C.R(C.M);
            Diagnostic(ErrorCode.ERR_BadRetType, "C.M").WithArguments("C.M()", "int[]").WithLocation(3, 5));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_MethodGroup_ParameterType(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.R(C.M);
            C.R(x => C.M(x));

            static class C
            {
                public static void M(Span<int> x) => Console.Write(x[1]);
                public static void R(D a) => a(new int[] { 1, 2, 3 });
            }

            delegate void D(int[] x);
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,5): error CS0123: No overload for 'C.M(Span<int>)' matches delegate 'D'
            // C.R(C.M);
            Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "C.M").WithArguments("C.M(System.Span<int>)", "D").WithLocation(3, 5));
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MethodGroup_ExtensionMethodReceiver()
    {
        var source = """
            using System;

            var a = new[] { 1, 2, 3 };
            C.R(a.M);
            C.R(x => a.M(x));

            static class C
            {
                public static int M(this Span<int> x, int y) => x[y];
                public static void R(Func<int, int> f) => Console.Write(f(1));
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,5): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Func<int, int>'
            // C.R(a.M);
            Diagnostic(ErrorCode.ERR_BadArgType, "a.M").WithArguments("1", "method group", "System.Func<int, int>").WithLocation(4, 5),
            // (5,10): error CS1929: 'int[]' does not contain a definition for 'M' and the best extension method overload 'C.M(Span<int>, int)' requires a receiver of type 'System.Span<int>'
            // C.R(x => a.M(x));
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("int[]", "M", "C.M(System.Span<int>, int)", "System.Span<int>").WithLocation(5, 10));

        var expectedDiagnostics = new[]
        {
            // (4,5): error CS1113: Extension method 'C.M(Span<int>, int)' defined on value type 'Span<int>' cannot be used to create delegates
            // C.R(a.M);
            Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "a.M").WithArguments("C.M(System.Span<int>, int)", "System.Span<int>").WithLocation(4, 5)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MethodGroup_ExtensionMethodReceiver_Inferred()
    {
        var source = """
            using System;

            var a = new[] { 1, 2, 3 };
            var d1 = a.M;
            var d2 = x => a.M(x);
            var d3 = (int x) => a.M(x);

            static class C
            {
                public static int M(this Span<int> x, int y) => x[y];
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,10): error CS8917: The delegate type could not be inferred.
            // var d1 = a.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "a.M").WithLocation(4, 10),
            // (5,10): error CS8917: The delegate type could not be inferred.
            // var d2 = x => a.M(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M(x)").WithLocation(5, 10),
            // (6,21): error CS1929: 'int[]' does not contain a definition for 'M' and the best extension method overload 'C.M(Span<int>, int)' requires a receiver of type 'System.Span<int>'
            // var d3 = (int x) => a.M(x);
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("int[]", "M", "C.M(System.Span<int>, int)", "System.Span<int>").WithLocation(6, 21));

        var expectedDiagnostics = new[]
        {
            // (4,10): error CS1113: Extension method 'C.M(Span<int>, int)' defined on value type 'Span<int>' cannot be used to create delegates
            // var d1 = a.M;
            Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "a.M").WithArguments("C.M(System.Span<int>, int)", "System.Span<int>").WithLocation(4, 10),
            // (5,10): error CS8917: The delegate type could not be inferred.
            // var d2 = x => a.M(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M(x)").WithLocation(5, 10)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);

        var tree = comp.SyntaxTrees.Single();
        var model = comp.GetSemanticModel(tree);
        var aVariable = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        var aSymbol = (ILocalSymbol)model.GetDeclaredSymbol(aVariable)!;
        AssertEx.Equal("System.Int32[]", aSymbol.Type.ToTestDisplayString());
        var d1Access = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .First(s => s.Expression.ToString() == "a");
        var lookupResult = model.LookupSymbols(d1Access.Name.SpanStart, aSymbol.Type, "M", includeReducedExtensionMethods: true);
        AssertEx.Equal("System.Int32 System.Span<System.Int32>.M(System.Int32 y)", lookupResult.Single().ToTestDisplayString());
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MethodGroup_ExtensionMethodReceiver_Generic()
    {
        var source = """
            using System;

            var a = new[] { 1, 2, 3 };
            C.R(a.M);
            C.R(a.M<int>);
            C.R(x => a.M(x));
            C.R(x => a.M<int>(x));

            static class C
            {
                public static T M<T>(this Span<T> x, int y) => x[y];
                public static void R(Func<int, int> f) => Console.Write(f(1));
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,5): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // C.R(a.M);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "a.M").WithArguments("int[]", "M").WithLocation(4, 5),
            // (5,5): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Func<int, int>'
            // C.R(a.M<int>);
            Diagnostic(ErrorCode.ERR_BadArgType, "a.M<int>").WithArguments("1", "method group", "System.Func<int, int>").WithLocation(5, 5),
            // (6,12): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // C.R(x => a.M(x));
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int[]", "M").WithLocation(6, 12),
            // (7,10): error CS1929: 'int[]' does not contain a definition for 'M' and the best extension method overload 'C.M<int>(Span<int>, int)' requires a receiver of type 'System.Span<int>'
            // C.R(x => a.M<int>(x));
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("int[]", "M", "C.M<int>(System.Span<int>, int)", "System.Span<int>").WithLocation(7, 10));

        // PROTOTYPE: Some of these need type inference to work.
        var expectedDiagnostics = new[]
        {
            // (4,5): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // C.R(a.M);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "a.M").WithArguments("int[]", "M").WithLocation(4, 5),
            // (5,5): error CS1113: Extension method 'C.M<int>(Span<int>, int)' defined on value type 'Span<int>' cannot be used to create delegates
            // C.R(a.M<int>);
            Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "a.M<int>").WithArguments("C.M<int>(System.Span<int>, int)", "System.Span<int>").WithLocation(5, 5),
            // (6,12): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // C.R(x => a.M(x));
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int[]", "M").WithLocation(6, 12)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Array_Span_Implicit_MethodGroup_ExtensionMethodReceiver_Generic_Inferred()
    {
        var source = """
            using System;

            var a = new[] { 1, 2, 3 };
            var d1 = a.M;
            var d2 = x => a.M(x);
            var d3 = (int x) => a.M(x);
            var d4 = a.M<int>;
            var d5 = x => a.M<int>(x);
            var d6 = (int x) => a.M<int>(x);

            static class C
            {
                public static T M<T>(this Span<T> x, int y) => x[y];
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,10): error CS8917: The delegate type could not be inferred.
            // var d1 = a.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "a.M").WithLocation(4, 10),
            // (5,10): error CS8917: The delegate type could not be inferred.
            // var d2 = x => a.M(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M(x)").WithLocation(5, 10),
            // (6,23): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // var d3 = (int x) => a.M(x);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int[]", "M").WithLocation(6, 23),
            // (7,10): error CS8917: The delegate type could not be inferred.
            // var d4 = a.M<int>;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "a.M<int>").WithLocation(7, 10),
            // (8,10): error CS8917: The delegate type could not be inferred.
            // var d5 = x => a.M<int>(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M<int>(x)").WithLocation(8, 10),
            // (9,21): error CS1929: 'int[]' does not contain a definition for 'M' and the best extension method overload 'C.M<int>(Span<int>, int)' requires a receiver of type 'System.Span<int>'
            // var d6 = (int x) => a.M<int>(x);
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("int[]", "M", "C.M<int>(System.Span<int>, int)", "System.Span<int>").WithLocation(9, 21));

        // PROTOTYPE: Some of these need type inference to work.
        var expectedDiagnostics = new[]
        {
            // (4,10): error CS8917: The delegate type could not be inferred.
            // var d1 = a.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "a.M").WithLocation(4, 10),
            // (5,10): error CS8917: The delegate type could not be inferred.
            // var d2 = x => a.M(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M(x)").WithLocation(5, 10),
            // (6,23): error CS1061: 'int[]' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            // var d3 = (int x) => a.M(x);
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int[]", "M").WithLocation(6, 23),
            // (7,10): error CS1113: Extension method 'C.M<int>(Span<int>, int)' defined on value type 'Span<int>' cannot be used to create delegates
            // var d4 = a.M<int>;
            Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "a.M<int>").WithArguments("C.M<int>(System.Span<int>, int)", "System.Span<int>").WithLocation(7, 10),
            // (8,10): error CS8917: The delegate type could not be inferred.
            // var d5 = x => a.M<int>(x);
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "x => a.M<int>(x)").WithLocation(8, 10)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_MethodGroup_FunctionPointer(LanguageVersion langVersion)
    {
        var source = """
            using System;

            unsafe
            {
                C.R(&C.M);
            }

            unsafe static class C
            {
                public static void M(Span<int> x) => Console.Write(x[1]);
                public static void R(delegate*<int[], void> a) => a(new int[] { 1, 2, 3 });
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, TestOptions.UnsafeReleaseExe, TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,9): error CS8757: No overload for 'C.M(Span<int>)' matches function pointer 'delegate*<int[], void>'
            //     C.R(&C.M);
            Diagnostic(ErrorCode.ERR_MethFuncPtrMismatch, "&C.M").WithArguments("C.M(System.Span<int>)", "delegate*<int[], void>").WithLocation(5, 9));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_ExpressionTree_01(
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        var source = $$"""
            using System;
            using System.Linq.Expressions;

            C.R(a => C.M(a));

            static class C
            {
                public static void R(Expression<Action<string[]>> e) => e.Compile()(new string[] { "a" });
                public static void M({{type}}<string> x) => Console.Write(x.Length + " " + x[0]);
            }
            """;

        var expectedOutput = "1 a";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", $$"""
            {
              // Code size      108 (0x6c)
              .maxstack  9
              .locals init (System.Linq.Expressions.ParameterExpression V_0)
              IL_0000:  ldtoken    "string[]"
              IL_0005:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_000a:  ldstr      "a"
              IL_000f:  call       "System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)"
              IL_0014:  stloc.0
              IL_0015:  ldnull
              IL_0016:  ldtoken    "void C.M(System.{{type}}<string>)"
              IL_001b:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_0020:  castclass  "System.Reflection.MethodInfo"
              IL_0025:  ldc.i4.1
              IL_0026:  newarr     "System.Linq.Expressions.Expression"
              IL_002b:  dup
              IL_002c:  ldc.i4.0
              IL_002d:  ldloc.0
              IL_002e:  ldtoken    "System.{{type}}<string>"
              IL_0033:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_0038:  ldtoken    "System.{{type}}<string> System.{{type}}<string>.op_Implicit(string[])"
              IL_003d:  ldtoken    "System.{{type}}<string>"
              IL_0042:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"
              IL_0047:  castclass  "System.Reflection.MethodInfo"
              IL_004c:  call       "System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)"
              IL_0051:  stelem.ref
              IL_0052:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
              IL_0057:  ldc.i4.1
              IL_0058:  newarr     "System.Linq.Expressions.ParameterExpression"
              IL_005d:  dup
              IL_005e:  ldc.i4.0
              IL_005f:  ldloc.0
              IL_0060:  stelem.ref
              IL_0061:  call       "System.Linq.Expressions.Expression<System.Action<string[]>> System.Linq.Expressions.Expression.Lambda<System.Action<string[]>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
              IL_0066:  call       "void C.R(System.Linq.Expressions.Expression<System.Action<string[]>>)"
              IL_006b:  ret
            }
            """);

        var expectedIl = $$"""
            {
              // Code size      108 (0x6c)
              .maxstack  11
              .locals init (System.Linq.Expressions.ParameterExpression V_0)
              IL_0000:  ldtoken    "string[]"
              IL_0005:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_000a:  ldstr      "a"
              IL_000f:  call       "System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)"
              IL_0014:  stloc.0
              IL_0015:  ldnull
              IL_0016:  ldtoken    "void C.M(System.{{type}}<string>)"
              IL_001b:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_0020:  castclass  "System.Reflection.MethodInfo"
              IL_0025:  ldc.i4.1
              IL_0026:  newarr     "System.Linq.Expressions.Expression"
              IL_002b:  dup
              IL_002c:  ldc.i4.0
              IL_002d:  ldnull
              IL_002e:  ldtoken    "System.{{type}}<string> System.{{type}}<string>.op_Implicit(string[])"
              IL_0033:  ldtoken    "System.{{type}}<string>"
              IL_0038:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"
              IL_003d:  castclass  "System.Reflection.MethodInfo"
              IL_0042:  ldc.i4.1
              IL_0043:  newarr     "System.Linq.Expressions.Expression"
              IL_0048:  dup
              IL_0049:  ldc.i4.0
              IL_004a:  ldloc.0
              IL_004b:  stelem.ref
              IL_004c:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
              IL_0051:  stelem.ref
              IL_0052:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
              IL_0057:  ldc.i4.1
              IL_0058:  newarr     "System.Linq.Expressions.ParameterExpression"
              IL_005d:  dup
              IL_005e:  ldc.i4.0
              IL_005f:  ldloc.0
              IL_0060:  stelem.ref
              IL_0061:  call       "System.Linq.Expressions.Expression<System.Action<string[]>> System.Linq.Expressions.Expression.Lambda<System.Action<string[]>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
              IL_0066:  call       "void C.R(System.Linq.Expressions.Expression<System.Action<string[]>>)"
              IL_006b:  ret
            }
            """;

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Implicit_ExpressionTree_02(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(() => C.M(null));

            static class C
            {
                public static void R(Expression<Action> e) { }
                public static void M(Span<string> x) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       97 (0x61)
              .maxstack  9
              IL_0000:  ldnull
              IL_0001:  ldtoken    "void C.M(System.Span<string>)"
              IL_0006:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
              IL_000b:  castclass  "System.Reflection.MethodInfo"
              IL_0010:  ldc.i4.1
              IL_0011:  newarr     "System.Linq.Expressions.Expression"
              IL_0016:  dup
              IL_0017:  ldc.i4.0
              IL_0018:  ldnull
              IL_0019:  ldtoken    "string[]"
              IL_001e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_0023:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
              IL_0028:  ldtoken    "System.Span<string>"
              IL_002d:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
              IL_0032:  ldtoken    "System.Span<string> System.Span<string>.op_Implicit(string[])"
              IL_0037:  ldtoken    "System.Span<string>"
              IL_003c:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"
              IL_0041:  castclass  "System.Reflection.MethodInfo"
              IL_0046:  call       "System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)"
              IL_004b:  stelem.ref
              IL_004c:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
              IL_0051:  call       "System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()"
              IL_0056:  call       "System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
              IL_005b:  call       "void C.R(System.Linq.Expressions.Expression<System.Action>)"
              IL_0060:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Implicit_ExpressionTree_02(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(() => C.M(null));

            static class C
            {
                public static void R(Expression<Action> e) { }
                public static void M(ReadOnlySpan<string> x) { }
            }
            """;

        var expectedIl = """
            {
                // Code size       97 (0x61)
                .maxstack  9
                IL_0000:  ldnull
                IL_0001:  ldtoken    "void C.M(System.ReadOnlySpan<string>)"
                IL_0006:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
                IL_000b:  castclass  "System.Reflection.MethodInfo"
                IL_0010:  ldc.i4.1
                IL_0011:  newarr     "System.Linq.Expressions.Expression"
                IL_0016:  dup
                IL_0017:  ldc.i4.0
                IL_0018:  ldnull
                IL_0019:  ldtoken    "string[]"
                IL_001e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_0023:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
                IL_0028:  ldtoken    "System.ReadOnlySpan<string>"
                IL_002d:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                IL_0032:  ldtoken    "System.ReadOnlySpan<string> System.ReadOnlySpan<string>.op_Implicit(string[])"
                IL_0037:  ldtoken    "System.ReadOnlySpan<string>"
                IL_003c:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"
                IL_0041:  castclass  "System.Reflection.MethodInfo"
                IL_0046:  call       "System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)"
                IL_004b:  stelem.ref
                IL_004c:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
                IL_0051:  call       "System.Linq.Expressions.ParameterExpression[] System.Array.Empty<System.Linq.Expressions.ParameterExpression>()"
                IL_0056:  call       "System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
                IL_005b:  call       "void C.R(System.Linq.Expressions.Expression<System.Action>)"
                IL_0060:  ret
            }
            """;

        if (ExecutionConditionUtil.IsCoreClr)
        {
            var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            var verifier = CompileAndVerify(comp).VerifyDiagnostics();
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
        }
        else
        {
            // The test ReadOnlySpan source contains a non-standard operator from `string` to `ReadOnlySpan<T>`
            // which is ambiguous with the one from `T[]` to `ReadOnlySpan<T>` resulting in this slightly misleading error.
            CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
                // (4,15): error CS0037: Cannot convert null to 'ReadOnlySpan<string>' because it is a non-nullable value type
                // C.R(() => C.M(null));
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.ReadOnlySpan<string>").WithLocation(4, 15));

            var source2 = """
                namespace System;

                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator ReadOnlySpan<T>(T[] array) => throw null;
                }
                """;

            var comp = CreateCompilation([source, source2], parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
            var verifier = CompileAndVerify(comp).VerifyDiagnostics();
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
        }
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Implicit_ExpressionTree_03(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        var source = $$"""
            using System;
            using System.Linq.Expressions;

            C.R(() => C.M(default));

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M({{type}}<string> x) => Console.Write(x.Length);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,15): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
            // C.R(() => C.M(default));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default").WithArguments(type).WithLocation(4, 15));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Span_ReadOnlySpan_Implicit_ExpressionTree(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(x => C.M(x));

            delegate void D(Span<string> x);

            static class C
            {
                public static void R(Expression<D> e) { }
                public static void M(ReadOnlySpan<string> x) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,5): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("Span").WithLocation(4, 5),
            // (4,14): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("Span").WithLocation(4, 14));
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_Implicit_ExpressionTree()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(x => C.M(x));

            delegate void D(Span<string> x);

            static class C
            {
                public static void R(Expression<D> e) { }
                public static void M(ReadOnlySpan<object> x) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,14): error CS1503: Argument 1: cannot convert from 'System.Span<string>' to 'System.ReadOnlySpan<object>'
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.Span<string>", "System.ReadOnlySpan<object>").WithLocation(4, 14));

        var expectedDiagnostics = new[]
        {
            // (4,5): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("Span").WithLocation(4, 5),
            // (4,14): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Span'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("Span").WithLocation(4, 14)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Implicit_ExpressionTree()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(x => C.M(x));

            delegate void D(ReadOnlySpan<string> x);

            static class C
            {
                public static void R(Expression<D> e) { }
                public static void M(ReadOnlySpan<object> x) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,14): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<string>' to 'System.ReadOnlySpan<object>'
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "System.ReadOnlySpan<string>", "System.ReadOnlySpan<object>").WithLocation(4, 14));

        var expectedDiagnostics = new[]
        {
            // (4,5): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("ReadOnlySpan").WithLocation(4, 5),
            // (4,14): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
            // C.R(x => C.M(x));
            Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "x").WithArguments("ReadOnlySpan").WithLocation(4, 14)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_string_ReadOnlySpan_Implicit_ExpressionTree(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(x => C.M(x));

            static class C
            {
                public static void R(Expression<Action<string>> e) => e.Compile()("s");
                public static void M(ReadOnlySpan<char> x) => Console.Write(x.Length + " " + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "1 s").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Covariant(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        bool cast)
    {
        var source = $$"""
            using System;

            class C
            {
                ReadOnlySpan<object> M(string[] x) => {{(cast ? "(ReadOnlySpan<object>)" : "")}}x;
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
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
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Interface_Covariant(
        [CombinatorialLangVersions] LanguageVersion langVersion,
        bool cast)
    {
        var source = $$"""
            using System;

            class C
            {
                ReadOnlySpan<I<object>> M(I<string>[] x) => {{(cast ? "(ReadOnlySpan<I<object>>)" : "")}}x;
            }

            interface I<out T> { }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,29): error CS9244: The type 'ReadOnlySpan<object>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'I<T>'
            //     I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "M").WithArguments("I<T>", "T", "System.ReadOnlySpan<object>").WithLocation(5, 29),
            // (5,49): error CS0266: Cannot implicitly convert type 'I<string[]>' to 'I<System.ReadOnlySpan<object>>'. An explicit conversion exists (are you missing a cast?)
            //     I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<string[]>", "I<System.ReadOnlySpan<object>>").WithLocation(5, 49));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_ReadOnlySpan_Interface_Outside_AllowsRefStruct(
        [CombinatorialValues("", "in", "out")] string variance)
    {
        var source = $$"""
            using System;

            class C
            {
                I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            }

            interface I<{{variance}} T> where T : allows ref struct { }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (5,49): error CS0266: Cannot implicitly convert type 'I<string[]>' to 'I<System.ReadOnlySpan<object>>'. An explicit conversion exists (are you missing a cast?)
            //     I<ReadOnlySpan<object>> M(I<string[]> x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<string[]>", "I<System.ReadOnlySpan<object>>").WithLocation(5, 49));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Interface_Invariant(
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        var source = $$"""
            using System;

            class C
            {
                {{type}}<I<object>> M(I<string>[] x)
                    => x;
            }

            interface I<T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,12): error CS0029: Cannot implicitly convert type 'I<string>[]' to 'System.Span<I<object>>'
            //         => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("I<string>[]", $"System.{type}<I<object>>").WithLocation(6, 12));

        var expectedDiagnostics = new[]
        {
            // (6,12): error CS0266: Cannot implicitly convert type 'I<string>[]' to 'System.Span<I<object>>'. An explicit conversion exists (are you missing a cast?)
            //         => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<string>[]", $"System.{type}<I<object>>").WithLocation(6, 12)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Interface_Cast(
        [CombinatorialValues("Span", "ReadOnlySpan")] string type)
    {
        var source = $$"""
            using System;

            class C
            {
                {{type}}<I<object>> M(I<string>[] x)
                    => ({{type}}<I<object>>)x;
            }

            interface I<T> { }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (6,12): error CS0030: Cannot convert type 'I<string>[]' to 'System.Span<I<object>>'
            //         => (Span<I<object>>)x;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, $"({type}<I<object>>)x").WithArguments("I<string>[]", $"System.{type}<I<object>>").WithLocation(6, 12));

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();

        verifier.VerifyIL("C.M", $$"""
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.1
              IL_0001:  castclass  "I<object>[]"
              IL_0006:  call       "System.{{type}}<I<object>> System.{{type}}<I<object>>.op_Implicit(I<object>[])"
              IL_000b:  ret
            }
            """);
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,38): error CS0029: Cannot implicitly convert type 'int[]' to 'System.ReadOnlySpan<long>'
            //     ReadOnlySpan<long> M(int[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int[]", "System.ReadOnlySpan<long>").WithLocation(5, 38));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_Covariant_TypeParameter(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C<T, U>
                where T : class, U
            {
                ReadOnlySpan<U> M(T[] x) => x;
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        var verifier = CompileAndVerify(comp, verify: Verification.FailsILVerify).VerifyDiagnostics();
        verifier.VerifyIL("C<T, U>.M", """
            {
              // Code size        9 (0x9)
              .maxstack  1
              .locals init (U[] V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  call       "System.ReadOnlySpan<U> System.ReadOnlySpan<U>.op_Implicit(U[])"
              IL_0008:  ret
            }
            """);
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_TypeParameter_NullableValueType_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C<T> where T : struct
            {
                ReadOnlySpan<T> M(T?[] x) => x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,34): error CS0029: Cannot implicitly convert type 'T?[]' to 'System.ReadOnlySpan<T>'
            //     ReadOnlySpan<T> M(T?[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T?[]", "System.ReadOnlySpan<T>").WithLocation(5, 34));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_ReadOnlySpan_TypeParameter_NullableValueType_02(LanguageVersion langVersion)
    {
        var source = """
            using System;

            class C<T> where T : struct
            {
                ReadOnlySpan<T?> M(T[] x) => x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (5,34): error CS0029: Cannot implicitly convert type 'T[]' to 'System.ReadOnlySpan<T?>'
            //     ReadOnlySpan<T?> M(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T[]", "System.ReadOnlySpan<T?>").WithLocation(5, 34));
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_Variance_01(
        [CombinatorialValues("", "where U : T")] string constraints,
        [CombinatorialLangVersions] LanguageVersion langVersion)
    {
        var source = $$"""
            using System;
            class C<T, U> {{constraints}}
            {
                ReadOnlySpan<U> F1(T[] x) => x;
                ReadOnlySpan<U> F2(T[] x) => (ReadOnlySpan<U>)x;
                Span<U> F3(T[] x) => x;
                Span<U> F4(T[] x) => (Span<U>)x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,34): error CS0029: Cannot implicitly convert type 'T[]' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F1(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T[]", "System.ReadOnlySpan<U>").WithLocation(4, 34),
            // (5,34): error CS0030: Cannot convert type 'T[]' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F2(T[] x) => (ReadOnlySpan<U>)x;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<U>)x").WithArguments("T[]", "System.ReadOnlySpan<U>").WithLocation(5, 34),
            // (6,26): error CS0029: Cannot implicitly convert type 'T[]' to 'System.Span<U>'
            //     Span<U> F3(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("T[]", "System.Span<U>").WithLocation(6, 26),
            // (7,26): error CS0030: Cannot convert type 'T[]' to 'System.Span<U>'
            //     Span<U> F4(T[] x) => (Span<U>)x;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(Span<U>)x").WithArguments("T[]", "System.Span<U>").WithLocation(7, 26));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_Variance_02(LanguageVersion langVersion)
    {
        var source = """
            using System;
            class C<T, U>
                where T : class
                where U : class, T
            {
                ReadOnlySpan<U> F1(T[] x) => x;
                ReadOnlySpan<U> F2(T[] x) => (ReadOnlySpan<U>)x;
                Span<U> F3(T[] x) => x;
                Span<U> F4(T[] x) => (Span<U>)x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (6,34): error CS0266: Cannot implicitly convert type 'T[]' to 'System.ReadOnlySpan<U>'. An explicit conversion exists (are you missing a cast?)
            //     ReadOnlySpan<U> F1(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("T[]", "System.ReadOnlySpan<U>").WithLocation(6, 34),
            // (8,26): error CS0266: Cannot implicitly convert type 'T[]' to 'System.Span<U>'. An explicit conversion exists (are you missing a cast?)
            //     Span<U> F3(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("T[]", "System.Span<U>").WithLocation(8, 26));
    }

    [Fact]
    public void Conversion_Array_Span_Variance_03()
    {
        var source = """
            using System;
            class C<T, U>
                where T : class, U
                where U : class
            {
                ReadOnlySpan<U> F1(T[] x) => x;
                ReadOnlySpan<U> F2(T[] x) => (ReadOnlySpan<U>)x;
                Span<U> F3(T[] x) => x;
                Span<U> F4(T[] x) => (Span<U>)x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics();

        // Note: although a breaking change, the previous would fail with a runtime exception
        // (Span's constructor checks that the element types are identical).

        var expectedDiagnostics = new[]
        {
            // (8,26): error CS0266: Cannot implicitly convert type 'T[]' to 'System.Span<U>'. An explicit conversion exists (are you missing a cast?)
            //     Span<U> F3(T[] x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("T[]", "System.Span<U>").WithLocation(8, 26)
        };

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_Variance()
    {
        var source = """
            using System;
            class C<T, U>
                where T : class
                where U : class, T
            {
                ReadOnlySpan<U> F1(Span<T> x) => x;
                ReadOnlySpan<U> F2(Span<T> x) => (ReadOnlySpan<U>)x;
                ReadOnlySpan<T> F3(Span<U> x) => x;
                ReadOnlySpan<T> F4(Span<U> x) => (ReadOnlySpan<T>)x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,38): error CS0029: Cannot implicitly convert type 'System.Span<T>' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F1(Span<T> x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.Span<T>", "System.ReadOnlySpan<U>").WithLocation(6, 38),
            // (7,38): error CS0030: Cannot convert type 'System.Span<T>' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F2(Span<T> x) => (ReadOnlySpan<U>)x;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<U>)x").WithArguments("System.Span<T>", "System.ReadOnlySpan<U>").WithLocation(7, 38));
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_Variance()
    {
        var source = """
            using System;
            class C<T, U>
                where T : class
                where U : class, T
            {
                ReadOnlySpan<U> F1(ReadOnlySpan<T> x) => x;
                ReadOnlySpan<U> F2(ReadOnlySpan<T> x) => (ReadOnlySpan<U>)x;
                ReadOnlySpan<T> F3(ReadOnlySpan<U> x) => x;
                ReadOnlySpan<T> F4(ReadOnlySpan<U> x) => (ReadOnlySpan<T>)x;
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (6,46): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<T>' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F1(ReadOnlySpan<T> x) => x;
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("System.ReadOnlySpan<T>", "System.ReadOnlySpan<U>").WithLocation(6, 46),
            // (7,46): error CS0030: Cannot convert type 'System.ReadOnlySpan<T>' to 'System.ReadOnlySpan<U>'
            //     ReadOnlySpan<U> F2(ReadOnlySpan<T> x) => (ReadOnlySpan<U>)x;
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(ReadOnlySpan<U>)x").WithArguments("System.ReadOnlySpan<T>", "System.ReadOnlySpan<U>").WithLocation(7, 46));
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

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.Span<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", $"System.{destination}<int>").WithLocation(3, 5));

        var expectedOutput = "456";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_ThroughUserImplicit()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator Span<int>(C c) => new Span<int>(new int[] { 4, 5, 6 });
            }

            static class D
            {
                public static void M(ReadOnlySpan<int> xs) => Console.Write(xs.Length + " " + xs[0]);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<int>").WithLocation(3, 5));

        var expectedOutput = "3 4";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_ThroughUserImplicit()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator Span<string>(C c) => new Span<string>(new string[] { "x" });
            }

            static class D
            {
                public static void M(ReadOnlySpan<object> xs) => Console.Write(xs.Length + " " + xs[0]);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<object>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<object>").WithLocation(3, 5));

        var expectedOutput = "1 x";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_ThroughUserImplicit()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator ReadOnlySpan<string>(C c) => new ReadOnlySpan<string>(new string[] { "x" });
            }

            static class D
            {
                public static void M(ReadOnlySpan<object> xs) => Console.Write(xs.Length + " " + xs[0]);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<object>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<object>").WithLocation(3, 5));

        var expectedOutput = "1 x";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.FailsILVerify).VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_ThroughUserImplicit()
    {
        var source = """
            using System;

            D.M(new C());

            class C
            {
                public static implicit operator string(C c) => "x";
            }

            static class D
            {
                public static void M(ReadOnlySpan<char> xs) => Console.Write(xs.Length + " " + xs[0]);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<char>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<char>").WithLocation(3, 5));

        var expectedOutput = "1 x";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_ThroughUserImplicit_Cast_01(
        [CombinatorialValues("Span", "ReadOnlySpan")] string destination,
        [CombinatorialValues("implicit", "explicit")] string op)
    {
        var source = $$"""
            using System;

            D.M(({{destination}}<int>)new C());

            class C
            {
                public static {{op}} operator int[](C c) => new int[] { 4, 5, 6 };
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

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS0030: Cannot convert type 'C' to 'System.Span<int>'
            // D.M((Span<int>)new C());
            Diagnostic(ErrorCode.ERR_NoExplicitConv, $"({destination}<int>)new C()").WithArguments("C", $"System.{destination}<int>").WithLocation(3, 5));

        var expectedOutput = "456";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Conversion_Array_Span_ThroughUserImplicit_Cast_02(
        [CombinatorialValues("Span", "ReadOnlySpan")] string destination,
        [CombinatorialValues("implicit", "explicit")] string op,
        [CombinatorialLangVersions] LanguageVersion langVersion)
    {
        var source = $$"""
            using System;

            D.M((int[])new C());

            class C
            {
                public static {{op}} operator int[](C c) => new int[] { 4, 5, 6 };
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
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "456").VerifyDiagnostics();
    }

    [Fact]
    public void Conversion_Array_Span_ThroughUserImplicit_MissingHelper()
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
                public static void M(Span<int> xs) { }
            }
            """;

        var missingRosHelper = """
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator Span<T>(T[] array) => throw null;
                }
                public readonly ref struct ReadOnlySpan<T>
                {
                }
            }
            """;

        var missingSpanHelper = """
            namespace System
            {
                public readonly ref struct Span<T>
                {
                }
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator ReadOnlySpan<T>(T[] array) => throw null;
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.Span<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.Span<int>").WithLocation(3, 5)
        };

        verifyWithMissing(missingRosHelper, TestOptions.Regular13, expectedDiagnostics);
        verifyWithMissing(missingSpanHelper, TestOptions.Regular13, expectedDiagnostics);

        expectedDiagnostics = [
            // (3,5): error CS0656: Missing compiler required member 'System.Span<T>.op_Implicit'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new C()").WithArguments("System.Span<T>", "op_Implicit").WithLocation(3, 5)
        ];

        verifyWithMissing(missingRosHelper, TestOptions.RegularNext);
        verifyWithMissing(missingSpanHelper, TestOptions.RegularNext, expectedDiagnostics);

        verifyWithMissing(missingRosHelper, TestOptions.RegularPreview);
        verifyWithMissing(missingSpanHelper, TestOptions.RegularPreview, expectedDiagnostics);

        void verifyWithMissing(string source2, CSharpParseOptions parseOptions, params DiagnosticDescription[] expected)
        {
            CreateCompilation([source, source2], parseOptions: parseOptions).VerifyDiagnostics(expected);
        }
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_ThroughUserImplicit_MissingHelper()
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
                public static void M(ReadOnlySpan<int> xs) { }
            }
            """;

        var missingRosHelper = """
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    public static implicit operator Span<T>(T[] array) => throw null;
                }
                public readonly ref struct ReadOnlySpan<T>
                {
                }
            }
            """;

        var missingSpanHelper = """
            namespace System
            {
                public readonly ref struct Span<T>
                {
                }
                public readonly ref struct ReadOnlySpan<T>
                {
                    public static implicit operator ReadOnlySpan<T>(T[] array) => throw null;
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (3,5): error CS1503: Argument 1: cannot convert from 'C' to 'System.ReadOnlySpan<int>'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C", "System.ReadOnlySpan<int>").WithLocation(3, 5)
        };

        verifyWithMissing(missingRosHelper, TestOptions.Regular13, expectedDiagnostics);
        verifyWithMissing(missingSpanHelper, TestOptions.Regular13, expectedDiagnostics);

        expectedDiagnostics = [
            // (3,5): error CS0656: Missing compiler required member 'System.ReadOnlySpan<T>.op_Implicit'
            // D.M(new C());
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new C()").WithArguments("System.ReadOnlySpan<T>", "op_Implicit").WithLocation(3, 5)
        ];

        verifyWithMissing(missingRosHelper, TestOptions.RegularNext, expectedDiagnostics);
        verifyWithMissing(missingSpanHelper, TestOptions.RegularNext);

        verifyWithMissing(missingRosHelper, TestOptions.RegularPreview, expectedDiagnostics);
        verifyWithMissing(missingSpanHelper, TestOptions.RegularPreview);

        void verifyWithMissing(string source2, CSharpParseOptions parseOptions, params DiagnosticDescription[] expected)
        {
            CreateCompilation([source, source2], parseOptions: parseOptions).VerifyDiagnostics(expected);
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,40): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<int>)' requires a receiver of type 'System.Span<int>'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", "C.E(System.Span<int>)", "System.Span<int>").WithLocation(7, 40));

        var expectedOutput = "8";

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  call       "void C.E(System.Span<int>)"
              IL_000b:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_ExtensionMethodReceiver_Implicit()
    {
        var source = """
            using System;

            C.M(new Span<int>(new int[] { 7, 8, 9 }));

            static class C
            {
                public static void M(Span<int> arg) => arg.E();
                public static void E(this ReadOnlySpan<int> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,44): error CS1929: 'Span<int>' does not contain a definition for 'E' and the best extension method overload 'C.E(ReadOnlySpan<int>)' requires a receiver of type 'System.ReadOnlySpan<int>'
            //     public static void M(Span<int> arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.Span<int>", "E", "C.E(System.ReadOnlySpan<int>)", "System.ReadOnlySpan<int>").WithLocation(7, 44));

        var expectedOutput = "8";

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<int> System.Span<int>.op_Implicit(System.Span<int>)"
              IL_0006:  call       "void C.E(System.ReadOnlySpan<int>)"
              IL_000b:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_Span_ReadOnlySpan_CastUp_ExtensionMethodReceiver_Implicit()
    {
        var source = """
            using System;

            C.M(new Span<string>(new string[] { "x", "y" }));

            static class C
            {
                public static void M(Span<string> arg) => arg.E();
                public static void E(this ReadOnlySpan<object> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,47): error CS1929: 'Span<string>' does not contain a definition for 'E' and the best extension method overload 'C.E(ReadOnlySpan<object>)' requires a receiver of type 'System.ReadOnlySpan<object>'
            //     public static void M(Span<string> arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.Span<string>", "E", "C.E(System.ReadOnlySpan<object>)", "System.ReadOnlySpan<object>").WithLocation(7, 47));

        var expectedOutput = "y";

        var expectedIl = """
            {
              // Code size       17 (0x11)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<string> System.Span<string>.op_Implicit(System.Span<string>)"
              IL_0006:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_000b:  call       "void C.E(System.ReadOnlySpan<object>)"
              IL_0010:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_ExtensionMethodReceiver_Implicit()
    {
        var source = """
            using System;

            C.M(new ReadOnlySpan<string>(new string[] { "x", "y" }));

            static class C
            {
                public static void M(ReadOnlySpan<string> arg) => arg.E();
                public static void E(this ReadOnlySpan<object> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,55): error CS1929: 'ReadOnlySpan<string>' does not contain a definition for 'E' and the best extension method overload 'C.E(ReadOnlySpan<object>)' requires a receiver of type 'System.ReadOnlySpan<object>'
            //     public static void M(ReadOnlySpan<string> arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.ReadOnlySpan<string>", "E", "C.E(System.ReadOnlySpan<object>)", "System.ReadOnlySpan<object>").WithLocation(7, 55));

        var expectedOutput = "y";

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.CastUp<string>(System.ReadOnlySpan<string>)"
              IL_0006:  call       "void C.E(System.ReadOnlySpan<object>)"
              IL_000b:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_string_ReadOnlySpan_ExtensionMethodReceiver_Implicit()
    {
        var source = """
            using System;

            C.M("xyz");

            static class C
            {
                public static void M(string arg) => arg.E();
                public static void E(this ReadOnlySpan<char> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,41): error CS1929: 'string' does not contain a definition for 'E' and the best extension method overload 'C.E(ReadOnlySpan<char>)' requires a receiver of type 'System.ReadOnlySpan<char>'
            //     public static void M(string arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("string", "E", "C.E(System.ReadOnlySpan<char>)", "System.ReadOnlySpan<char>").WithLocation(7, 41));

        var expectedOutput = "y";

        var expectedIl = """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  call       "void C.E(System.ReadOnlySpan<char>)"
              IL_000b:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
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
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
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
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  call       "void C.E({{modifier}} System.Span<int>)"
              IL_000e:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_MissingHelper()
    {
        var source = """
            using System;
            
            C.M(new int[] { 7, 8, 9 });
            
            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this Span<int> arg) { }
            }
            
            namespace System
            {
                public readonly ref struct Span<T>
                {
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (7,40): error CS0656: Missing compiler required member 'System.Span<T>.op_Implicit'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Span<T>", "op_Implicit").WithLocation(7, 40));
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Generic_01()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E<T>(this Span<T> arg) { }
            }
            """;
        // PROTOTYPE: Needs type inference to work.
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (4,44): error CS1061: 'int[]' does not contain a definition for 'E' and no accessible extension method 'E' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "E").WithArguments("int[]", "E").WithLocation(4, 44));
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Generic_02()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E<int>();
                public static void E<T>(this Span<T> arg) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       12 (0xc)
              .maxstack  1
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  call       "void C.E<int>(System.Span<int>)"
              IL_000b:  ret
            }
            """);

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("arg.E<int>()", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        var info = model.GetSymbolInfo(invocation);
        Assert.Equal("void System.Span<System.Int32>.E<System.Int32>()", info.Symbol!.ToTestDisplayString());

        var methodSymbol = (IMethodSymbol)info.Symbol!;
        var spanType = methodSymbol.ReceiverType!;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var unreducedSymbol = methodSymbol.ReducedFrom!;
        var reduced = unreducedSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal(methodSymbol, reduced);

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        // PROTOTYPE: This needs type inference to work.
        reduced = unreducedSymbol.ReduceExtensionMethod(arrayType);
        Assert.Null(reduced);
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Generic_03()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E(42);
                public static void E<T>(this Span<T> arg, T x) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  ldc.i4.s   42
              IL_0008:  call       "void C.E<int>(System.Span<int>, int)"
              IL_000d:  ret
            }
            """);

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("arg.E(42)", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        var info = model.GetSymbolInfo(invocation);
        Assert.Equal("void System.Span<System.Int32>.E<System.Int32>(System.Int32 x)", info.Symbol!.ToTestDisplayString());

        var methodSymbol = (IMethodSymbol)info.Symbol!;
        var spanType = methodSymbol.ReceiverType!;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var unreducedSymbol = methodSymbol.ReducedFrom!;
        var reduced = unreducedSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal(methodSymbol, reduced);

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        // PROTOTYPE: This needs type inference to work.
        reduced = unreducedSymbol.ReduceExtensionMethod(arrayType);
        Assert.Null(reduced);
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Generic_04()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E<int>(42);
                public static void E<T>(this Span<T> arg, T x) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  ldc.i4.s   42
              IL_0008:  call       "void C.E<int>(System.Span<int>, int)"
              IL_000d:  ret
            }
            """);

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("arg.E<int>(42)", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        var info = model.GetSymbolInfo(invocation);
        Assert.Equal("void System.Span<System.Int32>.E<System.Int32>(System.Int32 x)", info.Symbol!.ToTestDisplayString());

        var methodSymbol = (IMethodSymbol)info.Symbol!;
        var spanType = methodSymbol.ReceiverType!;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var unreducedSymbol = methodSymbol.ReducedFrom!;
        var reduced = unreducedSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal(methodSymbol, reduced);

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        // PROTOTYPE: This needs type inference to work.
        reduced = unreducedSymbol.ReduceExtensionMethod(arrayType);
        Assert.Null(reduced);
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Generic_05()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E("abc");
                public static void E<T>(this Span<int> arg, T x) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       17 (0x11)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  call       "System.Span<int> System.Span<int>.op_Implicit(int[])"
              IL_0006:  ldstr      "abc"
              IL_000b:  call       "void C.E<string>(System.Span<int>, string)"
              IL_0010:  ret
            }
            """);

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("""arg.E("abc")""", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        var info = model.GetSymbolInfo(invocation);
        Assert.Equal("void System.Span<System.Int32>.E<System.String>(System.String x)", info.Symbol!.ToTestDisplayString());

        var methodSymbol = (IMethodSymbol)info.Symbol!;
        var spanType = methodSymbol.ReceiverType!;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var unreducedSymbol = methodSymbol.ReducedFrom!;
        var reduced = unreducedSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal("void System.Span<System.Int32>.E<T>(T x)", reduced.ToTestDisplayString());

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        reduced = unreducedSymbol.ReduceExtensionMethod(arrayType);
        Assert.Equal("void System.Span<System.Int32>.E<T>(T x)", reduced.ToTestDisplayString());
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Reduced_01()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this Span<int> arg) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        var verifier = CompileAndVerify(comp).VerifyDiagnostics();
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

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("arg.E()", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        var info = model.GetSymbolInfo(invocation);
        Assert.Equal("void System.Span<System.Int32>.E()", info.Symbol!.ToTestDisplayString());

        var methodSymbol = (IMethodSymbol)info.Symbol!;
        var spanType = methodSymbol.ReceiverType!;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var unreducedSymbol = methodSymbol.ReducedFrom!;
        var reduced = unreducedSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal("void System.Span<System.Int32>.E()", reduced.ToTestDisplayString());

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        reduced = unreducedSymbol.ReduceExtensionMethod(arrayType);
        Assert.Equal("void System.Span<System.Int32>.E()", reduced.ToTestDisplayString());
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Implicit_Reduced_01_CSharp13()
    {
        var source = """
            using System;
            static class C
            {
                public static void M(int[] arg) => arg.E();
                public static void E(this Span<int> arg) { }
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (4,40): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<int>)' requires a receiver of type 'System.Span<int>'
            //     public static void M(int[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("int[]", "E", "C.E(System.Span<int>)", "System.Span<int>").WithLocation(4, 40));

        var tree = comp.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        Assert.Equal("arg.E()", invocation.ToString());

        var model = comp.GetSemanticModel(tree);
        Assert.Null(model.GetSymbolInfo(invocation).Symbol);

        var methodSymbol = comp.GetMember<MethodSymbol>("C.E").GetPublicSymbol();
        var spanType = methodSymbol.Parameters.Single().Type;
        Assert.Equal("System.Span<System.Int32>", spanType.ToTestDisplayString());

        // Reduce the extension method with Span receiver.
        var reduced = methodSymbol.ReduceExtensionMethod(spanType);
        Assert.Equal("void System.Span<System.Int32>.E()", reduced.ToTestDisplayString());

        var arrayType = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol().Parameters.Single().Type;
        Assert.Equal("System.Int32[]", arrayType.ToTestDisplayString());

        // Reduce the extension method with array receiver.
        reduced = methodSymbol.ReduceExtensionMethod(arrayType);
        Assert.Equal("void System.Span<System.Int32>.E()", reduced.ToTestDisplayString());
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Explicit(LanguageVersion langVersion)
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

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
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
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Opposite_Implicit(LanguageVersion langVersion)
    {
        var source = """
            static class C
            {
                static void M1(System.Span<int> arg) => arg.E1();
                static void M2(System.ReadOnlySpan<int> arg) => arg.E1();
                static void M3(System.ReadOnlySpan<string> arg) => arg.E2();
                static void M4(System.ReadOnlySpan<object> arg) => arg.E3();
                static void E1(this int[] arg) { }
                static void E2(this object[] arg) { }
                static void E3(this string[] arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,45): error CS1929: 'Span<int>' does not contain a definition for 'E1' and the best extension method overload 'C.E1(int[])' requires a receiver of type 'int[]'
            //     static void M1(System.Span<int> arg) => arg.E1();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.Span<int>", "E1", "C.E1(int[])", "int[]").WithLocation(3, 45),
            // (4,53): error CS1929: 'ReadOnlySpan<int>' does not contain a definition for 'E1' and the best extension method overload 'C.E1(int[])' requires a receiver of type 'int[]'
            //     static void M2(System.ReadOnlySpan<int> arg) => arg.E1();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.ReadOnlySpan<int>", "E1", "C.E1(int[])", "int[]").WithLocation(4, 53),
            // (5,56): error CS1929: 'ReadOnlySpan<string>' does not contain a definition for 'E2' and the best extension method overload 'C.E2(object[])' requires a receiver of type 'object[]'
            //     static void M3(System.ReadOnlySpan<string> arg) => arg.E2();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.ReadOnlySpan<string>", "E2", "C.E2(object[])", "object[]").WithLocation(5, 56),
            // (6,56): error CS1929: 'ReadOnlySpan<object>' does not contain a definition for 'E3' and the best extension method overload 'C.E3(string[])' requires a receiver of type 'string[]'
            //     static void M4(System.ReadOnlySpan<object> arg) => arg.E3();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("System.ReadOnlySpan<object>", "E3", "C.E3(string[])", "string[]").WithLocation(6, 56));
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Opposite_Explicit(LanguageVersion langVersion)
    {
        var source = """
            static class C
            {
                static void M1(System.Span<int> arg) => ((int[])arg).E1();
                static void M2(System.ReadOnlySpan<int> arg) => ((int[])arg).E1();
                static void M3(System.ReadOnlySpan<string> arg) => ((object[])arg).E2();
                static void M4(System.ReadOnlySpan<object> arg) => ((string[])arg).E3();
                static void E1(this int[] arg) { }
                static void E2(this object[] arg) { }
                static void E3(this string[] arg) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (3,46): error CS0030: Cannot convert type 'System.Span<int>' to 'int[]'
            //     static void M1(System.Span<int> arg) => ((int[])arg).E1();
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.Span<int>", "int[]").WithLocation(3, 46),
            // (4,54): error CS0030: Cannot convert type 'System.ReadOnlySpan<int>' to 'int[]'
            //     static void M2(System.ReadOnlySpan<int> arg) => ((int[])arg).E1();
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])arg").WithArguments("System.ReadOnlySpan<int>", "int[]").WithLocation(4, 54),
            // (5,57): error CS0030: Cannot convert type 'System.ReadOnlySpan<string>' to 'object[]'
            //     static void M3(System.ReadOnlySpan<string> arg) => ((object[])arg).E2();
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object[])arg").WithArguments("System.ReadOnlySpan<string>", "object[]").WithLocation(5, 57),
            // (6,57): error CS0030: Cannot convert type 'System.ReadOnlySpan<object>' to 'string[]'
            //     static void M4(System.ReadOnlySpan<object> arg) => ((string[])arg).E3();
            Diagnostic(ErrorCode.ERR_NoExplicitConv, "(string[])arg").WithArguments("System.ReadOnlySpan<object>", "string[]").WithLocation(6, 57));
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

    [Theory]
    [InlineData("")]
    [InlineData("where U : T")]
    [InlineData("where T : class  where U : class, T")]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Variance_01(string constraints)
    {
        var source = $$"""
            using System;
            static class Extensions
            {
                public static void M1<T>(this Span<T> arg) { }
                public static void M2<T>(this ReadOnlySpan<T> arg) { }
            }
            class C<T, U> {{constraints}}
            {
                static void F1(T[] a)
                {
                    a.M1<U>();
                    a.M2<U>();
                }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (11,9): error CS1929: 'T[]' does not contain a definition for 'M1' and the best extension method overload 'Extensions.M1<U>(Span<U>)' requires a receiver of type 'System.Span<U>'
            //         a.M1<U>();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("T[]", "M1", "Extensions.M1<U>(System.Span<U>)", "System.Span<U>").WithLocation(11, 9),
            // (12,9): error CS1929: 'T[]' does not contain a definition for 'M2' and the best extension method overload 'Extensions.M2<U>(ReadOnlySpan<U>)' requires a receiver of type 'System.ReadOnlySpan<U>'
            //         a.M2<U>();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("T[]", "M2", "Extensions.M2<U>(System.ReadOnlySpan<U>)", "System.ReadOnlySpan<U>").WithLocation(12, 9));
    }

    [Fact]
    public void Conversion_Array_Span_ExtensionMethodReceiver_Variance_02()
    {
        var source = """
            using System;
            static class Extensions
            {
                public static void M1<T>(this Span<T> arg) { }
                public static void M2<T>(this ReadOnlySpan<T> arg) { }
            }
            class C<T, U>
                where T : class, U
                where U : class
            {
                static void F1(T[] a)
                {
                    a.M1<U>();
                    a.M2<U>();
                }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (13,9): error CS1929: 'T[]' does not contain a definition for 'M1' and the best extension method overload 'Extensions.M1<U>(Span<U>)' requires a receiver of type 'System.Span<U>'
            //         a.M1<U>();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("T[]", "M1", "Extensions.M1<U>(System.Span<U>)", "System.Span<U>").WithLocation(13, 9));
    }

    [Fact]
    public void Conversion_Array_ReadOnlySpan_ExtensionMethodReceiver_Covariance()
    {
        var source = """
            using System;

            C.M(new string[] { "a", "b", "c" });

            static class C
            {
                public static void M(string[] arg) => arg.E();
                public static void E(this ReadOnlySpan<object> arg) => Console.Write(arg[1]);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (7,43): error CS1929: 'string[]' does not contain a definition for 'E' and the best extension method overload 'C.E(ReadOnlySpan<object>)' requires a receiver of type 'System.ReadOnlySpan<object>'
            //     public static void M(string[] arg) => arg.E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "arg").WithArguments("string[]", "E", "C.E(System.ReadOnlySpan<object>)", "System.ReadOnlySpan<object>").WithLocation(7, 43));

        var expectedOutput = "b";

        var expectedIl = """
            {
              // Code size       14 (0xe)
              .maxstack  1
              .locals init (object[] V_0)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloc.0
              IL_0003:  call       "System.ReadOnlySpan<object> System.ReadOnlySpan<object>.op_Implicit(object[])"
              IL_0008:  call       "void C.E(System.ReadOnlySpan<object>)"
              IL_000d:  ret
            }
            """;

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        verifier = CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
        verifier.VerifyIL("C.M", expectedIl);
    }

    [Theory, CombinatorialData]
    public void Conversion_Span_ReadOnlySpan_RefSafety(bool cast)
    {
        var source = $$"""
            using System;
            class C
            {
                ReadOnlySpan<string> M()
                {
                    Span<string> x = {{(cast ? "(Span<string>)" : "")}}["a"];
                    return x;
                }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (7,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x;
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(7, 16));
    }

    [Theory, CombinatorialData]
    public void Conversion_ReadOnlySpan_ReadOnlySpan_RefSafety(bool cast)
    {
        var source = $$"""
            using System;
            class C
            {
                ReadOnlySpan<object> M()
                {
                    ReadOnlySpan<string> x = {{(cast ? "(ReadOnlySpan<string>)" : "")}}["a"];
                    return x;
                }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
            // (7,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
            //         return x;
            Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(7, 16));
    }

    [Fact]
    public void OverloadResolution_SpanVsIEnumerable()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            var a = new int[0];
            C.M(a);

            static class C
            {
                public static void M(Span<int> x) => Console.Write(1);
                public static void M(IEnumerable<int> x) => Console.Write(2);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (5,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Span<int>)' and 'C.M(IEnumerable<int>)'
            // C.M(a);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Span<int>)", "C.M(System.Collections.Generic.IEnumerable<int>)").WithLocation(5, 3));

        var expectedOutput = "1";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_SpanVsIEnumerable_CollectionExpression(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Collections.Generic;

            C.M([]);

            static class C
            {
                public static void M(Span<int> x) => Console.Write(1);
                public static void M(IEnumerable<int> x) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsIEnumerable_Ctor()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            var a = new int[0];
            var c = new C(a);

            class C
            {
                public C(Span<int> x) => Console.Write(1);
                public C(IEnumerable<int> x) => Console.Write(2);
            }
            """;

        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (5,13): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(Span<int>)' and 'C.C(IEnumerable<int>)'
            // var c = new C(a);
            Diagnostic(ErrorCode.ERR_AmbigCall, "C").WithArguments("C.C(System.Span<int>)", "C.C(System.Collections.Generic.IEnumerable<int>)").WithLocation(5, 13));

        var expectedOutput = "1";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);

            static class C
            {
                public static void M(string[] x) => Console.Write(" s" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" o" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "sa oSystem.String[] oa").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_02(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M([..a]);
            C.M(["a"]);

            static class C
            {
                public static void M(string[] x) { }
                public static void M(ReadOnlySpan<object> x) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
            // (4,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(string[])' and 'C.M(ReadOnlySpan<object>)'
            // C.M([..a]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(string[])", "C.M(System.ReadOnlySpan<object>)").WithLocation(4, 3),
            // (5,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(string[])' and 'C.M(ReadOnlySpan<object>)'
            // C.M(["a"]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(string[])", "C.M(System.ReadOnlySpan<object>)").WithLocation(5, 3));
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_03()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);
            C.M(["a"]);

            static class C
            {
                public static void M(object[] x) => Console.Write(" a" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa rSystem.String[] ra ra ra").VerifyDiagnostics();

        var expectedOutput = "ra rSystem.String[] ra ra ra";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_04(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new object[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);

            static class C
            {
                public static void M(object[] x) => Console.Write(" a" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "aa rSystem.Object[] ra ra").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_05(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M(null);
            C.M(default);
            C.M(default(object[]));

            static class C
            {
                public static void M(object[] x) => Console.Write(1);
                public static void M(ReadOnlySpan<object> x) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExpressionTree_01(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            var a = new string[] { "a" };
            C.R(() => C.M(a));

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(string[] x) => Console.Write(" s" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" o" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "sa").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_ExpressionTree_02()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            var a = new string[] { "a" };
            C.R(() => C.M(a));

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(object[] x) => Console.Write(" a" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();

        var expectedOutput = "ra";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExpressionTree_03(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            var a = new object[] { "a" };
            C.R(() => C.M(a));

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(object[] x) => Console.Write(" a" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExpressionTree_04(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            C.R(() => C.M(null));
            C.R(() => C.M(default));
            C.R(() => C.M(default(object[])));

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(object[] x) => Console.Write(" 1" + (x?[0] ?? "null"));
                public static void M(ReadOnlySpan<object> x) => Console.Write(" 2" + x.Length);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "1null 1null 1null").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_Params_01()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M(a);
            C.M(a, a);
            C.M([a]);
            C.M([..a, a]);
            C.M("a");

            static class C
            {
                public static void M(params string[] x) => Console.Write(" s" + x[0]);
                public static void M(params ReadOnlySpan<object> x) => Console.Write(" o" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: "sa oSystem.String[] oSystem.String[] oa sa").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_Params_02()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M([..a]);
            C.M(["a"]);

            static class C
            {
                public static void M(params string[] x) { }
                public static void M(params ReadOnlySpan<object> x) { }
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source).VerifyDiagnostics(
            // (4,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params string[])' and 'C.M(params ReadOnlySpan<object>)'
            // C.M([..a]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params string[])", "C.M(params System.ReadOnlySpan<object>)").WithLocation(4, 3),
            // (5,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(params string[])' and 'C.M(params ReadOnlySpan<object>)'
            // C.M(["a"]);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(params string[])", "C.M(params System.ReadOnlySpan<object>)").WithLocation(5, 3));
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_Params_03()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);
            C.M(["a"]);

            static class C
            {
                public static void M(params object[] x) => Console.Write(" a" + x[0]);
                public static void M(params ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: "ra rSystem.String[] ra ra ra").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_Params_04()
    {
        var source = """
            using System;

            var a = new object[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);

            static class C
            {
                public static void M(params object[] x) => Console.Write(" a" + x[0]);
                public static void M(params ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: "aa rSystem.Object[] ra ra").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_Params_05()
    {
        var source = """
            using System;

            C.M(null);
            C.M(default);

            static class C
            {
                public static void M(params object[] x) => Console.Write(1);
                public static void M(params ReadOnlySpan<object> x) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: "11").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExtensionMethodReceiver_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            a.M();

            static class C
            {
                public static void M(this string[] x) => Console.Write(" s" + x[0]);
                public static void M(this ReadOnlySpan<object> x) => Console.Write(" o" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "sa").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_ExtensionMethodReceiver_02()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            a.M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(" o" + x[0]);
                public static void M(this ReadOnlySpan<string> x) => Console.Write(" s" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "oa").VerifyDiagnostics();

        var expectedOutput = "sa";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArray_ExtensionMethodReceiver_03()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            a.M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(" a" + x[0]);
                public static void M(this ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();

        var expectedOutput = "ra";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExtensionMethodReceiver_04(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new object[] { "a" };
            a.M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(" a" + x[0]);
                public static void M(this ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_ReadOnlySpanVsArray_ExtensionMethodReceiver_05(LanguageVersion langVersion)
    {
        var source = """
            using System;

            ((object[])null).M();
            default(object[]).M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(1);
                public static void M(this ReadOnlySpan<object> x) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "11").VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_SpanVsArray_ExtensionMethodReceiver_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            a.M();

            static class C
            {
                public static void M(this string[] x) => Console.Write(" a" + x[0]);
                public static void M(this Span<string> x) => Console.Write(" s" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsArray_ExtensionMethodReceiver_02()
    {
        var source = """
            using System;

            var a = new string[] { "a" };
            a.M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(" a" + x[0]);
                public static void M(this Span<string> x) => Console.Write(" s" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();

        var expectedOutput = "sa";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_SpanVsArray_ExtensionMethodReceiver_ExpressionTree_01(LanguageVersion langVersion)
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            var a = new string[] { "a" };
            C.R(() => a.M());

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(this string[] x) => Console.Write(" a" + x[0]);
                public static void M(this Span<string> x) => Console.Write(" s" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsArray_ExtensionMethodReceiver_ExpressionTree_02()
    {
        var source = """
            using System;
            using System.Linq.Expressions;

            var a = new string[] { "a" };
            C.R(() => a.M());

            static class C
            {
                public static void R(Expression<Action> e) => e.Compile()();
                public static void M(this object[] x) => Console.Write(" a" + x[0]);
                public static void M(this Span<string> x) => Console.Write(" s" + x[0]);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa").VerifyDiagnostics();

        var expectedOutput = "sa";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_SpanVsReadOnlySpan_01(LanguageVersion langVersion)
    {
        var source = """
            using System;

            C.M(new int[0]);
            C.M(default(Span<int>));
            C.M(default(ReadOnlySpan<int>));

            static class C
            {
                public static void M(Span<int> arg) => Console.Write(1);
                public static void M(ReadOnlySpan<int> arg) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "112").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsReadOnlySpan_02()
    {
        var source = """
            using System;

            C.M(new object[0]);
            C.M(default(Span<object>));
            C.M(default(ReadOnlySpan<object>));

            try { C.M(new string[0]); } catch (ArrayTypeMismatchException) { Console.Write(3); }

            static class C
            {
                public static void M(Span<object> arg) => Console.Write(1);
                public static void M(ReadOnlySpan<object> arg) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsCoreClr ? "1123" : "1121").VerifyDiagnostics();

        var expectedOutput = "1122";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsReadOnlySpan_03()
    {
        var source = """
            using System;

            C.M(default(Span<string>));
            C.M(default(ReadOnlySpan<string>));

            static class C
            {
                public static void M(Span<object> arg) => Console.Write(1);
                public static void M(ReadOnlySpan<object> arg) => Console.Write(2);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,5): error CS1503: Argument 1: cannot convert from 'System.Span<string>' to 'System.Span<object>'
            // C.M(default(Span<string>));
            Diagnostic(ErrorCode.ERR_BadArgType, "default(Span<string>)").WithArguments("1", "System.Span<string>", "System.Span<object>").WithLocation(3, 5),
            // (4,5): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<string>' to 'System.Span<object>'
            // C.M(default(ReadOnlySpan<string>));
            Diagnostic(ErrorCode.ERR_BadArgType, "default(ReadOnlySpan<string>)").WithArguments("1", "System.ReadOnlySpan<string>", "System.Span<object>").WithLocation(4, 5));

        var expectedOutput = "22";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsReadOnlySpan_ExtensionMethodReceiver_01()
    {
        var source = """
            using System;

            (new int[0]).E();

            static class C
            {
                public static void E(this Span<int> arg) => Console.Write(1);
                public static void E(this ReadOnlySpan<int> arg) => Console.Write(2);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,2): error CS1929: 'int[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<int>)' requires a receiver of type 'System.Span<int>'
            // (new int[0]).E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new int[0]").WithArguments("int[]", "E", "C.E(System.Span<int>)", "System.Span<int>").WithLocation(3, 2));

        var expectedOutput = "1";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Theory, MemberData(nameof(LangVersions))]
    public void OverloadResolution_SpanVsReadOnlySpan_ExtensionMethodReceiver_02(LanguageVersion langVersion)
    {
        var source = """
            using System;

            default(Span<int>).E();
            default(ReadOnlySpan<int>).E();

            static class C
            {
                public static void E(this Span<int> arg) => Console.Write(1);
                public static void E(this ReadOnlySpan<int> arg) => Console.Write(2);
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion));
        CompileAndVerify(comp, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsReadOnlySpan_ExtensionMethodReceiver_03()
    {
        var source = """
            using System;

            (new string[0]).E();
            (new object[0]).E();

            static class C
            {
                public static void E(this Span<object> arg) => Console.Write(1);
                public static void E(this ReadOnlySpan<object> arg) => Console.Write(2);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,2): error CS1929: 'string[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<object>)' requires a receiver of type 'System.Span<object>'
            // (new string[0]).E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new string[0]").WithArguments("string[]", "E", "C.E(System.Span<object>)", "System.Span<object>").WithLocation(3, 2),
            // (4,2): error CS1929: 'object[]' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<object>)' requires a receiver of type 'System.Span<object>'
            // (new string[0]).E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new object[0]").WithArguments("object[]", "E", "C.E(System.Span<object>)", "System.Span<object>").WithLocation(4, 2));

        var expectedOutput = "21";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_SpanVsReadOnlySpan_ExtensionMethodReceiver_04()
    {
        var source = """
            using System;

            default(Span<string>).E();
            default(ReadOnlySpan<string>).E();

            static class C
            {
                public static void E(this Span<object> arg) => Console.Write(1);
                public static void E(this ReadOnlySpan<object> arg) => Console.Write(2);
            }
            """;
        CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
            // (3,1): error CS1929: 'Span<string>' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<object>)' requires a receiver of type 'System.Span<object>'
            // default(Span<string>).E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "default(Span<string>)").WithArguments("System.Span<string>", "E", "C.E(System.Span<object>)", "System.Span<object>").WithLocation(3, 1),
            // (4,1): error CS1929: 'ReadOnlySpan<string>' does not contain a definition for 'E' and the best extension method overload 'C.E(Span<object>)' requires a receiver of type 'System.Span<object>'
            // default(ReadOnlySpan<string>).E();
            Diagnostic(ErrorCode.ERR_BadInstanceArgType, "default(ReadOnlySpan<string>)").WithArguments("System.ReadOnlySpan<string>", "E", "C.E(System.Span<object>)", "System.Span<object>").WithLocation(4, 1));

        var expectedOutput = "22";

        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, targetFramework: TargetFramework.Net90);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArrayVsSpan()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            var a = new string[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);
            C.M(["a"]);

            var b = new object[] { "b" };
            C.M(b);
            C.M([b]);
            C.M([..b, b]);
            C.M([..b]);

            static class C
            {
                public static void M(object[] x) => Console.Write(" a" + x[0]);
                public static void M(ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
                public static void M(Span<object> x) => Console.Write(" s" + x[0]);
                public static void M(IEnumerable<object> x) => Console.Write(" e" + x.First());
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa rSystem.String[] ra ra ra ab rSystem.Object[] rb rb").VerifyDiagnostics();

        var expectedOutput = "ra rSystem.String[] ra ra ra ab rSystem.Object[] rb rb";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArrayVsSpan_Params()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            var a = new string[] { "a" };
            C.M(a);
            C.M([a]);
            C.M([..a, a]);
            C.M([..a]);
            C.M(["a"]);

            var b = new object[] { "b" };
            C.M(b);
            C.M([b]);
            C.M([..b, b]);
            C.M([..b]);

            static class C
            {
                public static void M(params object[] x) => Console.Write(" a" + x[0]);
                public static void M(params ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
                public static void M(params Span<object> x) => Console.Write(" s" + x[0]);
                public static void M(params IEnumerable<object> x) => Console.Write(" e" + x.First());
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: "ra rSystem.String[] ra ra ra ab rSystem.Object[] rb rb").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadResolution_ReadOnlySpanVsArrayVsSpan_ExtensionMethodReceiver()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            var a = new string[] { "a" };
            a.M();

            var b = new object[] { "b" };
            b.M();

            static class C
            {
                public static void M(this object[] x) => Console.Write(" a" + x[0]);
                public static void M(this ReadOnlySpan<object> x) => Console.Write(" r" + x[0]);
                public static void M(this Span<object> x) => Console.Write(" s" + x[0]);
                public static void M(this IEnumerable<object> x) => Console.Write(" e" + x.First());
            }
            """;
        var comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.Regular13);
        CompileAndVerify(comp, expectedOutput: "aa ab").VerifyDiagnostics();

        var expectedOutput = "ra ab";

        comp = CreateCompilationWithSpanAndMemoryExtensions(source, parseOptions: TestOptions.RegularNext);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();

        comp = CreateCompilationWithSpanAndMemoryExtensions(source);
        CompileAndVerify(comp, expectedOutput: expectedOutput).VerifyDiagnostics();
    }
}
