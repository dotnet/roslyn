﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordTests : CSharpTestBase
    {
        private static TargetFramework GetTargetFramework(bool useInit) => useInit ? TargetFramework.Net80 : TargetFramework.Standard;

        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        private static string IncludeExpectedOutput(bool useInit, string expectedOutput) => !useInit ? expectedOutput : null;

        [Fact]
        public void Field_01()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P => field = 1;
                    public static object Q { get => field = 2; }
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((new C().P, C.Q));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                (1, 2)
                <P>k__BackingField: False
                <Q>k__BackingField: False
                """);
            verifier.VerifyIL("C.P.get", """
                {
                  // Code size       16 (0x10)
                  .maxstack  3
                  .locals init (object V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  box        "int"
                  IL_0007:  dup
                  IL_0008:  stloc.0
                  IL_0009:  stfld      "object C.<P>k__BackingField"
                  IL_000e:  ldloc.0
                  IL_000f:  ret
                }
                """);
            verifier.VerifyIL("C.Q.get", """
                {
                  // Code size       13 (0xd)
                  .maxstack  2
                  IL_0000:  ldc.i4.2
                  IL_0001:  box        "int"
                  IL_0006:  dup
                  IL_0007:  stsfld     "object C.<Q>k__BackingField"
                  IL_000c:  ret
                }
                """);
            var comp = (CSharpCompilation)verifier.Compilation;
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Object C.<P>k__BackingField",
                "System.Object C.P { get; }",
                "System.Object C.P.get",
                "System.Object C.<Q>k__BackingField",
                "System.Object C.Q { get; }",
                "System.Object C.Q.get",
                "C..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Field_02()
        {
            string source = """
                using System;
                class C
                {
                    public object P => Initialize(out field, 1);
                    public object Q { get => Initialize(out field, 2); }
                    static object Initialize(out object field, object value)
                    {
                        field = value;
                        return field;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P, c.Q));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2)");
            verifier.VerifyIL("C.P.get", """
                {
                  // Code size       18 (0x12)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "object C.<P>k__BackingField"
                  IL_0006:  ldc.i4.1
                  IL_0007:  box        "int"
                  IL_000c:  call       "object C.Initialize(out object, object)"
                  IL_0011:  ret
                }
                """);
            verifier.VerifyIL("C.Q.get", """
                {
                  // Code size       18 (0x12)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "object C.<Q>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  box        "int"
                  IL_000c:  call       "object C.Initialize(out object, object)"
                  IL_0011:  ret
                }
                """);
            var comp = (CSharpCompilation)verifier.Compilation;
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Object C.<P>k__BackingField",
                "System.Object C.P { get; }",
                "System.Object C.P.get",
                "System.Object C.<Q>k__BackingField",
                "System.Object C.Q { get; }",
                "System.Object C.Q.get",
                "System.Object C.Initialize(out System.Object field, System.Object value)",
                "C..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Field_03()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P { get { return field; } init { field = 1; } }
                    public object Q { init { field = 2; } }
                }
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net80, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("""
                <P>k__BackingField: False
                <Q>k__BackingField: True
                """));
        }

        [Fact]
        public void FieldReference_01()
        {
            string source = """
                class C
                {
                    static C _other = new();
                    object P
                    {
                        get { return _other.field; }
                        set { _ = field; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,29): error CS1061: 'C' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         get { return _other.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C", "field").WithLocation(6, 29));
        }

        [Fact]
        public void FieldReference_02()
        {
            string source = """
                class C
                {
                    C P
                    {
                        get { return null; }
                        set { field = value.field; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,29): error CS1061: 'C' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         set { field = value.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C", "field").WithLocation(6, 29));
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C C.<P>k__BackingField",
                "C C.P { get; set; }",
                "C C.P.get",
                "void C.P.set",
                "C..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void FieldReference_03()
        {
            string source = """
                class C
                {
                    int P
                    {
                        get { return field; }
                        set { _ = this is { field: 0 }; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,29): error CS0117: 'C' does not contain a definition for 'field'
                //         set { _ = this is { field: 0 }; }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("C", "field").WithLocation(6, 29));
        }

        [Fact]
        public void FieldInInitializer_01()
        {
            string source = """
                class C
                {
                    object P { get; } = F(field);
                    static object F(object value) => value;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS0103: The name 'field' does not exist in the current context
                //     object P { get; } = F(field);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 27));
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Object C.<P>k__BackingField",
                "System.Object C.P { get; }",
                "System.Object C.P.get",
                "System.Object C.F(System.Object value)",
                "C..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void FieldInInitializer_02()
        {
            string source = """
                class C
                {
                    object P { get => field; } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,34): error CS0103: The name 'field' does not exist in the current context
                //     object P { get => field; } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 34));
        }

        [Fact]
        public void FieldInInitializer_03()
        {
            string source = """
                class C
                {
                    object P { set { } } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,28): error CS0103: The name 'field' does not exist in the current context
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 28));
        }

        [Fact]
        public void Lambda_01()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P1 => F(() => field);
                    public object P2 { set { F(() => field = value); } }
                    public static object P3 => F(static () => field);
                    static object F(Func<object> f) => f();
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((new C().P1, C.P3));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}", field.Name);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                (, )
                <P1>k__BackingField
                <P2>k__BackingField
                <P3>k__BackingField
                """);
            verifier.VerifyIL("C.<get_P1>b__2_0()", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "object C.<P1>k__BackingField"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("C.<>c__DisplayClass5_0.<set_P2>b__0()", """
                {
                  // Code size       21 (0x15)
                  .maxstack  3
                  .locals init (object V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "C C.<>c__DisplayClass5_0.<>4__this"
                  IL_0006:  ldarg.0
                  IL_0007:  ldfld      "object C.<>c__DisplayClass5_0.value"
                  IL_000c:  dup
                  IL_000d:  stloc.0
                  IL_000e:  stfld      "object C.<P2>k__BackingField"
                  IL_0013:  ldloc.0
                  IL_0014:  ret
                }
                """);
            verifier.VerifyIL("C.<>c.<get_P3>b__8_0()", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  ldsfld     "object C.<P3>k__BackingField"
                  IL_0005:  ret
                }
                """);
        }

        [Fact]
        public void Lambda_02()
        {
            string source = """
                using System;
                class C
                {
                    public object P => F(static () => field);
                    static object F(Func<object> f) => f();
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,39): error CS8821: A static anonymous function cannot contain a reference to 'this' or 'base'.
                //     public object P => F(static () => field);
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis, "field").WithLocation(4, 39));
        }

        [Fact]
        public void LocalFunction_01()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P
                    {
                        get
                        {
                            object F() => field;
                            return F();
                        }
                    }
                    public static object Q
                    {
                        get
                        {
                            object F() => field;
                            return F();
                        }
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((new C().P, C.Q));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}", field.Name);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                (, )
                <P>k__BackingField
                <Q>k__BackingField
                """);
            verifier.VerifyIL("C.<get_P>g__F|2_0()", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "object C.<P>k__BackingField"
                  IL_0006:  ret
                }
                """);
            verifier.VerifyIL("C.<get_Q>g__F|5_0()", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  ldsfld     "object C.<Q>k__BackingField"
                  IL_0005:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_01(
            [CombinatorialValues("class", "struct", "ref struct", "record", "record struct")] string typeKind,
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = $$"""
                {{typeKind}} A
                {
                    public static object P1 { get; set { _ = field; } }
                    public static object P2 { get { return field; } set; }
                    public static object P3 { get { return null; } set; }
                    public object Q1 { get; set { _ = field; } }
                    public object Q2 { get { return field; } set; }
                    public object Q3 { get { return field; } init; }
                    public object Q4 { get; set { } }
                    public object Q5 { get; init { } }
                }
                class Program
                {
                    static void Main()
                    {
                        _ = new A();
                    }
                }
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,26): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static object P1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P1").WithArguments("field keyword").WithLocation(3, 26),
                    // (3,46): error CS0103: The name 'field' does not exist in the current context
                    //     public static object P1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 46),
                    // (4,26): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static object P2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P2").WithArguments("field keyword").WithLocation(4, 26),
                    // (4,44): error CS0103: The name 'field' does not exist in the current context
                    //     public static object P2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(4, 44),
                    // (5,26): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static object P3 { get { return null; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P3").WithArguments("field keyword").WithLocation(5, 26),
                    // (6,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public object Q1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Q1").WithArguments("field keyword").WithLocation(6, 19),
                    // (6,39): error CS0103: The name 'field' does not exist in the current context
                    //     public object Q1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(6, 39),
                    // (7,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public object Q2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Q2").WithArguments("field keyword").WithLocation(7, 19),
                    // (7,37): error CS0103: The name 'field' does not exist in the current context
                    //     public object Q2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(7, 37),
                    // (8,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public object Q3 { get { return field; } init; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Q3").WithArguments("field keyword").WithLocation(8, 19),
                    // (8,37): error CS0103: The name 'field' does not exist in the current context
                    //     public object Q3 { get { return field; } init; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 37),
                    // (9,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public object Q4 { get; set { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Q4").WithArguments("field keyword").WithLocation(9, 19),
                    // (10,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public object Q5 { get; init { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Q5").WithArguments("field keyword").WithLocation(10, 19));
            }
            else
            {
                var verifier = CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput(""));
                verifier.VerifyIL("A.P1.get", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  ldsfld     "object A.<P1>k__BackingField"
                  IL_0005:  ret
                }
                """);
                verifier.VerifyIL("A.P2.set", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  stsfld     "object A.<P2>k__BackingField"
                  IL_0006:  ret
                }
                """);
                verifier.VerifyIL("A.Q1.get", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  ldfld      "object A.<Q1>k__BackingField"
                  IL_0006:  ret
                }
                """);
                verifier.VerifyIL("A.Q2.set", """
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stfld      "object A.<Q2>k__BackingField"
                  IL_0007:  ret
                }
                """);
                verifier.VerifyIL("A.Q3.init", """
                {
                  // Code size        8 (0x8)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldarg.1
                  IL_0002:  stfld      "object A.<Q3>k__BackingField"
                  IL_0007:  ret
                }
                """);
            }

            if (!typeKind.StartsWith("record"))
            {
                var actualMembers = comp.GetMember<NamedTypeSymbol>("A").GetMembers().ToTestDisplayStrings();
                string readonlyQualifier = typeKind.EndsWith("struct") ? "readonly " : "";
                var expectedMembers = new[]
                {
                    "System.Object A.<P1>k__BackingField",
                    "System.Object A.P1 { get; set; }",
                    "System.Object A.P1.get",
                    "void A.P1.set",
                    "System.Object A.<P2>k__BackingField",
                    "System.Object A.P2 { get; set; }",
                    "System.Object A.P2.get",
                    "void A.P2.set",
                    "System.Object A.<P3>k__BackingField",
                    "System.Object A.P3 { get; set; }",
                    "System.Object A.P3.get",
                    "void A.P3.set",
                    "System.Object A.<Q1>k__BackingField",
                    "System.Object A.Q1 { get; set; }",
                    readonlyQualifier + "System.Object A.Q1.get",
                    "void A.Q1.set",
                    "System.Object A.<Q2>k__BackingField",
                    "System.Object A.Q2 { get; set; }",
                    "System.Object A.Q2.get",
                    "void A.Q2.set",
                    "System.Object A.<Q3>k__BackingField",
                    "System.Object A.Q3 { get; init; }",
                    "System.Object A.Q3.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.Q3.init",
                    "System.Object A.<Q4>k__BackingField",
                    "System.Object A.Q4 { get; set; }",
                    readonlyQualifier + "System.Object A.Q4.get",
                    "void A.Q4.set",
                    "System.Object A.<Q5>k__BackingField",
                    "System.Object A.Q5 { get; init; }",
                    readonlyQualifier + "System.Object A.Q5.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.Q5.init",
                    "A..ctor()"
                };
                AssertEx.Equal(expectedMembers, actualMembers);
            }
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = """
                interface I
                {
                    static object P1 { get; set { _ = field; } }
                    static object P2 { get { return field; } set; }
                }
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                targetFramework: TargetFramework.Net80);

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     static object P1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P1").WithArguments("field keyword").WithLocation(3, 19),
                    // (3,39): error CS0103: The name 'field' does not exist in the current context
                    //     static object P1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 39),
                    // (4,19): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     static object P2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P2").WithArguments("field keyword").WithLocation(4, 19),
                    // (4,37): error CS0103: The name 'field' does not exist in the current context
                    //     static object P2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(4, 37));
            }
            else
            {
                var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
                verifier.VerifyIL("I.P1.get", """
                {
                  // Code size        6 (0x6)
                  .maxstack  1
                  IL_0000:  ldsfld     "object I.<P1>k__BackingField"
                  IL_0005:  ret
                }
                """);
                verifier.VerifyIL("I.P2.set", """
                {
                  // Code size        7 (0x7)
                  .maxstack  1
                  IL_0000:  ldarg.0
                  IL_0001:  stsfld     "object I.<P2>k__BackingField"
                  IL_0006:  ret
                }
                """);
            }

            var actualMembers = comp.GetMember<NamedTypeSymbol>("I").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
                {
                    "System.Object I.<P1>k__BackingField",
                    "System.Object I.P1 { get; set; }",
                    "System.Object I.P1.get",
                    "void I.P1.set",
                    "System.Object I.<P2>k__BackingField",
                    "System.Object I.P2 { get; set; }",
                    "System.Object I.P2.get",
                    "void I.P2.set",
                };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_03(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = """
                interface I
                {
                    object Q1 { get; set { _ = field; } }
                    object Q2 { get { return field; } set; }
                    object Q3 { get { return field; } init; }
                }
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                targetFramework: TargetFramework.Net80);

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,17): error CS0501: 'I.Q1.get' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I.Q1.get").WithLocation(3, 17),
                    // (3,32): error CS0103: The name 'field' does not exist in the current context
                    //     object Q1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 32),
                    // (4,30): error CS0103: The name 'field' does not exist in the current context
                    //     object Q2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(4, 30),
                    // (4,39): error CS0501: 'I.Q2.set' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I.Q2.set").WithLocation(4, 39),
                    // (5,30): error CS0103: The name 'field' does not exist in the current context
                    //     object Q3 { get { return field; } init; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(5, 30),
                    // (5,39): error CS0501: 'I.Q3.init' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q3 { get { return field; } init; }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "init").WithArguments("I.Q3.init").WithLocation(5, 39));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,17): error CS0501: 'I.Q1.get' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q1 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("I.Q1.get").WithLocation(3, 17),
                    // (4,39): error CS0501: 'I.Q2.set' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q2 { get { return field; } set; }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "set").WithArguments("I.Q2.set").WithLocation(4, 39),
                    // (5,39): error CS0501: 'I.Q3.init' must declare a body because it is not marked abstract, extern, or partial
                    //     object Q3 { get { return field; } init; }
                    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "init").WithArguments("I.Q3.init").WithLocation(5, 39));
            }

            var actualMembers = comp.GetMember<NamedTypeSymbol>("I").GetMembers().ToTestDisplayStrings();
            string[] expectedMembers;
            if (languageVersion == LanguageVersion.CSharp13)
            {
                expectedMembers = new[]
                    {
                        "System.Object I.Q1 { get; set; }",
                        "System.Object I.Q1.get",
                        "void I.Q1.set",
                        "System.Object I.Q2 { get; set; }",
                        "System.Object I.Q2.get",
                        "void I.Q2.set",
                        "System.Object I.Q3 { get; init; }",
                        "System.Object I.Q3.get",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) I.Q3.init",
                    };
            }
            else
            {
                expectedMembers = new[]
                    {
                        "System.Object I.<Q1>k__BackingField",
                        "System.Object I.Q1 { get; set; }",
                        "System.Object I.Q1.get",
                        "void I.Q1.set",
                        "System.Object I.<Q2>k__BackingField",
                        "System.Object I.Q2 { get; set; }",
                        "System.Object I.Q2.get",
                        "void I.Q2.set",
                        "System.Object I.<Q3>k__BackingField",
                        "System.Object I.Q3 { get; init; }",
                        "System.Object I.Q3.get",
                        "void modreq(System.Runtime.CompilerServices.IsExternalInit) I.Q3.init",
                    };
            }
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_04(
            [CombinatorialValues("class", "struct", "ref struct", "record", "record struct")] string typeKind,
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = $$"""
                {{typeKind}} A
                {
                    public static int P1 { get; set { } }
                    public static int P2 { get { return -2; } set; }
                    public int P3 { get; set { } }
                    public int P4 { get { return -4; } set; }
                    public int P5 { get; init { } }
                    public int P6 { get { return -6; } init; }
                }
                class Program
                {
                    static void Main()
                    {
                        A.P1 = 1;
                        A.P2 = 2;
                        var a = new A() { P3 = 3, P4 = 4, P5 = 5, P6 = 6 };
                        System.Console.WriteLine((A.P1, A.P2, a.P3, a.P4, a.P5, a.P6));
                    }
                }
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,23): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static int P1 { get; set { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P1").WithArguments("field keyword").WithLocation(3, 23),
                    // (4,23): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static int P2 { get { return -2; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P2").WithArguments("field keyword").WithLocation(4, 23),
                    // (5,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P3 { get; set { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P3").WithArguments("field keyword").WithLocation(5, 16),
                    // (6,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P4 { get { return -4; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P4").WithArguments("field keyword").WithLocation(6, 16),
                    // (7,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P5 { get; init { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P5").WithArguments("field keyword").WithLocation(7, 16),
                    // (8,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P6 { get { return -6; } init; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P6").WithArguments("field keyword").WithLocation(8, 16));
            }
            else
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("(0, -2, 0, -4, 0, -6)"));
            }

            if (!typeKind.StartsWith("record"))
            {
                var actualMembers = comp.GetMember<NamedTypeSymbol>("A").GetMembers().ToTestDisplayStrings();
                string readonlyQualifier = typeKind.EndsWith("struct") ? "readonly " : "";
                var expectedMembers = new[]
                    {
                    "System.Int32 A.<P1>k__BackingField",
                    "System.Int32 A.P1 { get; set; }",
                    "System.Int32 A.P1.get",
                    "void A.P1.set",
                    "System.Int32 A.<P2>k__BackingField",
                    "System.Int32 A.P2 { get; set; }",
                    "System.Int32 A.P2.get",
                    "void A.P2.set",
                    "System.Int32 A.<P3>k__BackingField",
                    "System.Int32 A.P3 { get; set; }",
                    readonlyQualifier + "System.Int32 A.P3.get",
                    "void A.P3.set",
                    "System.Int32 A.<P4>k__BackingField",
                    "System.Int32 A.P4 { get; set; }",
                    "System.Int32 A.P4.get",
                    "void A.P4.set",
                    "System.Int32 A.<P5>k__BackingField",
                    "System.Int32 A.P5 { get; init; }",
                    readonlyQualifier + "System.Int32 A.P5.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.P5.init",
                    "System.Int32 A.<P6>k__BackingField",
                    "System.Int32 A.P6 { get; init; }",
                    "System.Int32 A.P6.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.P6.init",
                    "A..ctor()",
                };
                AssertEx.Equal(expectedMembers, actualMembers);
            }
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_05(
            [CombinatorialValues("class", "struct", "ref struct", "record", "record struct")] string typeKind,
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion)
        {
            string source = $$"""
                {{typeKind}} A
                {
                    public static int P1 { get; set { field = value * 2; } }
                    public static int P2 { get { return field * -1; } set; }
                    public int P3 { get; set { field = value * 2; } }
                    public int P4 { get { return field * -1; } set; }
                    public int P5 { get; init { field = value * 2; } }
                    public int P6 { get { return field * -1; } init; }
                }
                class Program
                {
                    static void Main()
                    {
                        A.P1 = 1;
                        A.P2 = 2;
                        var a = new A() { P3 = 3, P4 = 4, P5 = 5, P6 = 6 };
                        System.Console.WriteLine((A.P1, A.P2, a.P3, a.P4, a.P5, a.P6));
                    }
                }
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: TargetFramework.Net80);

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,23): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static int P1 { get; set { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P1").WithArguments("field keyword").WithLocation(3, 23),
                    // (3,39): error CS0103: The name 'field' does not exist in the current context
                    //     public static int P1 { get; set { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 39),
                    // (4,23): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public static int P2 { get { return field * -1; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P2").WithArguments("field keyword").WithLocation(4, 23),
                    // (4,41): error CS0103: The name 'field' does not exist in the current context
                    //     public static int P2 { get { return field * -1; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(4, 41),
                    // (5,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P3 { get; set { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P3").WithArguments("field keyword").WithLocation(5, 16),
                    // (5,32): error CS0103: The name 'field' does not exist in the current context
                    //     public int P3 { get; set { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(5, 32),
                    // (6,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P4 { get { return field * -1; } set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P4").WithArguments("field keyword").WithLocation(6, 16),
                    // (6,34): error CS0103: The name 'field' does not exist in the current context
                    //     public int P4 { get { return field * -1; } set; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(6, 34),
                    // (7,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P5 { get; init { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P5").WithArguments("field keyword").WithLocation(7, 16),
                    // (7,33): error CS0103: The name 'field' does not exist in the current context
                    //     public int P5 { get; init { field = value * 2; } }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(7, 33),
                    // (8,16): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public int P6 { get { return field * -1; } init; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P6").WithArguments("field keyword").WithLocation(8, 16),
                    // (8,34): error CS0103: The name 'field' does not exist in the current context
                    //     public int P6 { get { return field * -1; } init; }
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 34));
            }
            else
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("(2, -2, 6, -4, 10, -6)"));
            }

            if (!typeKind.StartsWith("record"))
            {
                var actualMembers = comp.GetMember<NamedTypeSymbol>("A").GetMembers().ToTestDisplayStrings();
                string readonlyQualifier = typeKind.EndsWith("struct") ? "readonly " : "";
                var expectedMembers = new[]
                    {
                    "System.Int32 A.<P1>k__BackingField",
                    "System.Int32 A.P1 { get; set; }",
                    "System.Int32 A.P1.get",
                    "void A.P1.set",
                    "System.Int32 A.<P2>k__BackingField",
                    "System.Int32 A.P2 { get; set; }",
                    "System.Int32 A.P2.get",
                    "void A.P2.set",
                    "System.Int32 A.<P3>k__BackingField",
                    "System.Int32 A.P3 { get; set; }",
                    readonlyQualifier + "System.Int32 A.P3.get",
                    "void A.P3.set",
                    "System.Int32 A.<P4>k__BackingField",
                    "System.Int32 A.P4 { get; set; }",
                    "System.Int32 A.P4.get",
                    "void A.P4.set",
                    "System.Int32 A.<P5>k__BackingField",
                    "System.Int32 A.P5 { get; init; }",
                    readonlyQualifier + "System.Int32 A.P5.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.P5.init",
                    "System.Int32 A.<P6>k__BackingField",
                    "System.Int32 A.P6 { get; init; }",
                    "System.Int32 A.P6.get",
                    "void modreq(System.Runtime.CompilerServices.IsExternalInit) A.P6.init",
                    "A..ctor()",
                };
                AssertEx.Equal(expectedMembers, actualMembers);
            }
        }

        [Fact]
        public void Attribute_01()
        {
            string source = """
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                class B
                {
                    [A(field)] object P1 { get { return null; } set { } }
                }
                class C
                {
                    const object field = null;
                    [A(field)] object P2 { get { return null; } set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,8): error CS0103: The name 'field' does not exist in the current context
                //     [A(field)] object P1 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 8));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var attributeArguments = tree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Select(arg => arg.Expression).ToArray();

            var argument = attributeArguments[0];
            Assert.IsType<IdentifierNameSyntax>(argument);
            Assert.Null(model.GetSymbolInfo(argument).Symbol);

            argument = attributeArguments[1];
            Assert.IsType<IdentifierNameSyntax>(argument);
            Assert.Equal("System.Object C.field", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void Attribute_02()
        {
            string source = """
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                class B
                {
                    object P1 { [A(field)] get { return null; } set { } }
                    object P2 { get { return null; } [A(field)] set { } }
                }
                class C
                {
                    const object field = null;
                    object P3 { [A(field)] get { return null; } set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,20): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     object P1 { [A(field)] get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(8, 20),
                // (9,41): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     object P2 { get { return null; } [A(field)] set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(9, 41),
                // (14,20): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                //     object P3 { [A(field)] get { return null; } set { } }
                Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments("preview").WithLocation(14, 20),
                // (14,20): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     object P3 { [A(field)] get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(14, 20));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var attributeArguments = tree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Select(arg => arg.Expression).ToArray();

            var argument = attributeArguments[0];
            Assert.IsType<FieldExpressionSyntax>(argument);
            Assert.Equal("System.Object B.<P1>k__BackingField", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());

            argument = attributeArguments[1];
            Assert.IsType<FieldExpressionSyntax>(argument);
            Assert.Equal("System.Object B.<P2>k__BackingField", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());

            argument = attributeArguments[2];
            Assert.IsType<FieldExpressionSyntax>(argument);
            Assert.Equal("System.Object C.<P3>k__BackingField", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void Attribute_03()
        {
            string source = $$"""
                using System;
                using System.Reflection;

                [AttributeUsage(AttributeTargets.All, AllowMultiple=true)]
                class A : Attribute
                {
                    private readonly object _obj;
                    public A(object obj) { _obj = obj; }
                    public override string ToString() => $"A({_obj})";
                }

                class B
                {
                    [A(0)][field: A(1)] public object P1 { get; }
                    [field: A(2)][field: A(-2)] public static object P2 { get; set; }
                    [field: A(3)] public object P3 { get; init; }
                    public object P4 { [field: A(4)] get; }
                    public static object P5 { get; [field: A(5)] set; }
                    [A(0)][field: A(1)] public object Q1 => field;
                    [field: A(2)][field: A(-2)] public static object Q2 { get { return field; } set { } }
                    [field: A(3)] public object Q3 { get { return field; } init { } }
                    public object Q4 { [field: A(4)] get => field; }
                    public static object Q5 { get { return field; } [field: A(5)] set { } }
                    [field: A(6)] public static object Q6 { set { _ = field; } }
                    [field: A(7)] public object Q7 { init { _ = field; } }
                }

                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(B).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            ReportField(field);
                    }

                    static void ReportField(FieldInfo field)
                    {
                        Console.Write("{0}.{1}:", field.DeclaringType.Name, field.Name);
                        foreach (var obj in field.GetCustomAttributes())
                            Console.Write(" {0},", obj.ToString());
                        Console.WriteLine();
                    }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (17,25): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                //     public object P4 { [field: A(4)] get; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return").WithLocation(17, 25),
                // (18,37): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //     public static object P5 { get; [field: A(5)] set; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(18, 37),
                // (22,25): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                //     public object Q4 { [field: A(4)] get => field; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return").WithLocation(22, 25),
                // (23,54): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //     public static object Q5 { get { return field; } [field: A(5)] set { } }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(23, 54));

            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("""
                B.<P1>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(1),
                B.<P3>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(3),
                B.<P4>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                B.<Q1>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(1),
                B.<Q3>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(3),
                B.<Q4>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                B.<Q7>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(7),
                B.<P2>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(2), A(-2),
                B.<P5>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                B.<Q2>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(2), A(-2),
                B.<Q5>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                B.<Q6>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(6),
                """));
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_01A([CombinatorialValues("class", "struct", "ref struct", "interface")] string typeKind)
        {
            string source = $$"""
                using System;
                {{typeKind}} C
                {
                    public static int P1 { get; } = 1;
                    public static int P2 { get => field; } = 2;
                    public static int P3 { get => field; set; } = 3;
                    public static int P4 { get => field; set { } } = 4;
                    public static int P5 { get => 0; set; } = 5;
                    public static int P6 { get; set; } = 6;
                    public static int P7 { get; set { } } = 7;
                    public static int P8 { set { field = value; } } = 8;
                    public static int P9 { get { return field; } set { field = value; } } = 9;
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P4, C.P5, C.P6, C.P7, C.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("(1, 2, 3, 4, 0, 6, 7, 9)"));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       56 (0x38)
                  .maxstack  1
                  IL_0000:  ldc.i4.1
                  IL_0001:  stsfld     "int C.<P1>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  stsfld     "int C.<P2>k__BackingField"
                  IL_000c:  ldc.i4.3
                  IL_000d:  stsfld     "int C.<P3>k__BackingField"
                  IL_0012:  ldc.i4.4
                  IL_0013:  stsfld     "int C.<P4>k__BackingField"
                  IL_0018:  ldc.i4.5
                  IL_0019:  stsfld     "int C.<P5>k__BackingField"
                  IL_001e:  ldc.i4.6
                  IL_001f:  stsfld     "int C.<P6>k__BackingField"
                  IL_0024:  ldc.i4.7
                  IL_0025:  stsfld     "int C.<P7>k__BackingField"
                  IL_002a:  ldc.i4.8
                  IL_002b:  stsfld     "int C.<P8>k__BackingField"
                  IL_0030:  ldc.i4.s   9
                  IL_0032:  stsfld     "int C.<P9>k__BackingField"
                  IL_0037:  ret
                }
                """);
        }

        [Fact]
        public void Initializer_01B()
        {
            string source = """
                using System;
                interface C
                {
                    public static int P1 { get; } = 1;
                    public static int P2 { get => field; } = 2;
                    public static int P3 { get => field; set; } = 3;
                    public static int P4 { get => field; set { } } = 4;
                    public static int P5 { get => 0; set; } = 5;
                    public static int P6 { get; set; } = 6;
                    public static int P7 { get; set { } } = 7;
                    public static int P8 { set { field = value; } } = 8;
                    public static int P9 { get { return field; } set { field = value; } } = 9;
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P4, C.P5, C.P6, C.P7, C.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("(1, 2, 3, 4, 0, 6, 7, 9)"));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       56 (0x38)
                  .maxstack  1
                  IL_0000:  ldc.i4.1
                  IL_0001:  stsfld     "int C.<P1>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  stsfld     "int C.<P2>k__BackingField"
                  IL_000c:  ldc.i4.3
                  IL_000d:  stsfld     "int C.<P3>k__BackingField"
                  IL_0012:  ldc.i4.4
                  IL_0013:  stsfld     "int C.<P4>k__BackingField"
                  IL_0018:  ldc.i4.5
                  IL_0019:  stsfld     "int C.<P5>k__BackingField"
                  IL_001e:  ldc.i4.6
                  IL_001f:  stsfld     "int C.<P6>k__BackingField"
                  IL_0024:  ldc.i4.7
                  IL_0025:  stsfld     "int C.<P7>k__BackingField"
                  IL_002a:  ldc.i4.8
                  IL_002b:  stsfld     "int C.<P8>k__BackingField"
                  IL_0030:  ldc.i4.s   9
                  IL_0032:  stsfld     "int C.<P9>k__BackingField"
                  IL_0037:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_02A(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                using System;
                class C
                {
                    public int P1 { get; } = 1;
                    public int P2 { get => field; } = 2;
                    public int P3 { get => field; {{setter}}; } = 3;
                    public int P4 { get => field; {{setter}} { } } = 4;
                    public int P5 { get => 0; {{setter}}; } = 5;
                    public int P6 { get; {{setter}}; } = 6;
                    public int P7 { get; {{setter}} { } } = 7;
                    public int P8 { {{setter}} { field = value; } } = 8;
                    public int P9 { get { return field; } {{setter}} { field = value; } } = 9;
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2, c.P3, c.P4, c.P5, c.P6, c.P7, c.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, "(1, 2, 3, 4, 0, 6, 7, 9)"));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..ctor", """
                {
                  // Code size       71 (0x47)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  stfld      "int C.<P1>k__BackingField"
                  IL_0007:  ldarg.0
                  IL_0008:  ldc.i4.2
                  IL_0009:  stfld      "int C.<P2>k__BackingField"
                  IL_000e:  ldarg.0
                  IL_000f:  ldc.i4.3
                  IL_0010:  stfld      "int C.<P3>k__BackingField"
                  IL_0015:  ldarg.0
                  IL_0016:  ldc.i4.4
                  IL_0017:  stfld      "int C.<P4>k__BackingField"
                  IL_001c:  ldarg.0
                  IL_001d:  ldc.i4.5
                  IL_001e:  stfld      "int C.<P5>k__BackingField"
                  IL_0023:  ldarg.0
                  IL_0024:  ldc.i4.6
                  IL_0025:  stfld      "int C.<P6>k__BackingField"
                  IL_002a:  ldarg.0
                  IL_002b:  ldc.i4.7
                  IL_002c:  stfld      "int C.<P7>k__BackingField"
                  IL_0031:  ldarg.0
                  IL_0032:  ldc.i4.8
                  IL_0033:  stfld      "int C.<P8>k__BackingField"
                  IL_0038:  ldarg.0
                  IL_0039:  ldc.i4.s   9
                  IL_003b:  stfld      "int C.<P9>k__BackingField"
                  IL_0040:  ldarg.0
                  IL_0041:  call       "object..ctor()"
                  IL_0046:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_02B(bool useRefStruct, bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string typeKind = useRefStruct ? "ref struct" : "struct";
            string source = $$"""
                using System;
                {{typeKind}} C
                {
                    public int P1 { get; } = 1;
                    public int P2 { get => field; } = 2;
                    public int P3 { get => field; {{setter}}; } = 3;
                    public int P4 { get => field; {{setter}} { } } = 4;
                    public int P5 { get => 0; {{setter}}; } = 5;
                    public int P6 { get; {{setter}}; } = 6;
                    public int P7 { get; {{setter}} { } } = 7;
                    public int P8 { {{setter}} { field = value; } } = 8;
                    public int P9 { get { return field; } {{setter}} { field = value; } } = 9;
                    public C() { }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2, c.P3, c.P4, c.P5, c.P6, c.P7, c.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, "(1, 2, 3, 4, 0, 6, 7, 9)"));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..ctor", """
                {
                  // Code size       65 (0x41)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  stfld      "int C.<P1>k__BackingField"
                  IL_0007:  ldarg.0
                  IL_0008:  ldc.i4.2
                  IL_0009:  stfld      "int C.<P2>k__BackingField"
                  IL_000e:  ldarg.0
                  IL_000f:  ldc.i4.3
                  IL_0010:  stfld      "int C.<P3>k__BackingField"
                  IL_0015:  ldarg.0
                  IL_0016:  ldc.i4.4
                  IL_0017:  stfld      "int C.<P4>k__BackingField"
                  IL_001c:  ldarg.0
                  IL_001d:  ldc.i4.5
                  IL_001e:  stfld      "int C.<P5>k__BackingField"
                  IL_0023:  ldarg.0
                  IL_0024:  ldc.i4.6
                  IL_0025:  stfld      "int C.<P6>k__BackingField"
                  IL_002a:  ldarg.0
                  IL_002b:  ldc.i4.7
                  IL_002c:  stfld      "int C.<P7>k__BackingField"
                  IL_0031:  ldarg.0
                  IL_0032:  ldc.i4.8
                  IL_0033:  stfld      "int C.<P8>k__BackingField"
                  IL_0038:  ldarg.0
                  IL_0039:  ldc.i4.s   9
                  IL_003b:  stfld      "int C.<P9>k__BackingField"
                  IL_0040:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_02C(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                using System;
                interface C
                {
                    public int P1 { get; } = 1;
                    public int P2 { get => field; } = 2;
                    public int P3 { get => field; {{setter}}; } = 3;
                    public int P4 { get => field; {{setter}} { } } = 4;
                    public int P5 { get => 0; {{setter}}; } = 5;
                    public int P6 { get; {{setter}}; } = 6;
                    public int P7 { get; {{setter}} { } } = 7;
                    public int P8 { {{setter}} { field = value; } } = 8;
                    public int P9 { get { return field; } {{setter}} { field = value; } } = 9;
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (4,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P1 { get; } = 1;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P1").WithLocation(4, 16),
                // (5,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P2 { get => field; } = 2;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P2").WithLocation(5, 16),
                // (6,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P3 { get => field; set; } = 3;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P3").WithLocation(6, 16),
                // (6,35): error CS0501: 'C.P3.set' must declare a body because it is not marked abstract, extern, or partial
                //     public int P3 { get => field; set; } = 3;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, setter).WithArguments($"C.P3.{setter}").WithLocation(6, 35),
                // (7,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P4 { get => field; set { } } = 4;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P4").WithLocation(7, 16),
                // (8,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P5 { get => 0; set; } = 5;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P5").WithLocation(8, 16),
                // (8,31): error CS0501: 'C.P5.set' must declare a body because it is not marked abstract, extern, or partial
                //     public int P5 { get => 0; set; } = 5;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, setter).WithArguments($"C.P5.{setter}").WithLocation(8, 31),
                // (9,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P6 { get; set; } = 6;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P6").WithLocation(9, 16),
                // (10,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P7 { get; set { } } = 7;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P7").WithLocation(10, 16),
                // (10,21): error CS0501: 'C.P7.get' must declare a body because it is not marked abstract, extern, or partial
                //     public int P7 { get; set { } } = 7;
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("C.P7.get").WithLocation(10, 21),
                // (11,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P8 { set { field = value; } } = 8;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P8").WithLocation(11, 16),
                // (12,16): error CS8053: Instance properties in interfaces cannot have initializers.
                //     public int P9 { get { return field; } set { field = value; } } = 9;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P9").WithLocation(12, 16));
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_02D(bool useRefStruct, bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string typeKind = useRefStruct ? "ref struct" : "    struct";
            string source = $$"""
                {{typeKind}} S1
                {
                    public int P1 { get; } = 1;
                }
                {{typeKind}} S2
                {
                    public int P2 { get => field; } = 2;
                }
                {{typeKind}} S3
                {
                    public int P3 { get => field; {{setter}}; } = 3;
                }
                {{typeKind}} S6
                {
                    public int P6 { get; {{setter}}; } = 6;
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (1,12): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     struct S1
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S1").WithLocation(1, 12),
                // (5,12): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     struct S2
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S2").WithLocation(5, 12),
                // (9,12): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     struct S3
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S3").WithLocation(9, 12),
                // (13,12): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                //     struct S6
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S6").WithLocation(13, 12));
        }

        [Fact]
        public void Initializer_03()
        {
            string source = """
                class C
                {
                    public static int PA { get => 0; } = 10;
                    public static int PB { get => 0; set { } } = 11;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,23): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //     public static int PA { get => 0; } = 10;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "PA").WithLocation(3, 23),
                // (4,23): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //     public static int PB { get => 0; set { } } = 11;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "PB").WithLocation(4, 23));
        }

        [Theory]
        [CombinatorialData]
        public void Initializer_04(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                class C
                {
                    public int PA { get => 0; } = 10;
                    public int PB { get => 0; {{setter}} { } } = 11;
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,16): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //     public int PA { get => 0; } = 10;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "PA").WithLocation(3, 16),
                // (4,16): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //     public int PB { get => 0; set { } } = 11;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "PB").WithLocation(4, 16));
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorAssignment_01([CombinatorialValues("class", "struct", "ref struct", "interface")] string typeKind)
        {
            string source = $$"""
                using System;
                {{typeKind}} C
                {
                    public static int P1 { get; }
                    public static int P2 { get => field; }
                    public static int P3 { get => field; set; }
                    public static int P4 { get => field; set { } }
                    public static int P5 { get => 0; set; }
                    public static int P6 { get; set; }
                    public static int P7 { get; set { } }
                    public static int P8 { set { field = value; } }
                    public static int P9 { get { return field; } set { field = value; } }
                    static C()
                    {
                        P1 = 1;
                        P2 = 2;
                        P3 = 3;
                        P4 = 4;
                        P5 = 5;
                        P6 = 6;
                        P7 = 7;
                        P8 = 8;
                        P9 = 9;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P4, C.P5, C.P6, C.P7, C.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("(1, 2, 3, 0, 0, 6, 0, 9)"));
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       56 (0x38)
                  .maxstack  1
                  IL_0000:  ldc.i4.1
                  IL_0001:  stsfld     "int C.<P1>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  stsfld     "int C.<P2>k__BackingField"
                  IL_000c:  ldc.i4.3
                  IL_000d:  call       "void C.P3.set"
                  IL_0012:  ldc.i4.4
                  IL_0013:  call       "void C.P4.set"
                  IL_0018:  ldc.i4.5
                  IL_0019:  call       "void C.P5.set"
                  IL_001e:  ldc.i4.6
                  IL_001f:  call       "void C.P6.set"
                  IL_0024:  ldc.i4.7
                  IL_0025:  call       "void C.P7.set"
                  IL_002a:  ldc.i4.8
                  IL_002b:  call       "void C.P8.set"
                  IL_0030:  ldc.i4.s   9
                  IL_0032:  call       "void C.P9.set"
                  IL_0037:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorAssignment_02([CombinatorialValues("class", "struct", "ref struct")] string typeKind, bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                using System;
                {{typeKind}} C
                {
                    public int P1 { get; }
                    public int P2 { get => field; }
                    public int P3 { get => field; {{setter}}; }
                    public int P4 { get => field; {{setter}} { } }
                    public int P5 { get => 0; {{setter}}; }
                    public int P6 { get; {{setter}}; }
                    public int P7 { get; {{setter}} { } }
                    public int P8 { {{setter}} { field = value; } }
                    public int P9 { get { return field; } {{setter}} { field = value; } }
                    public C()
                    {
                        P1 = 1;
                        P2 = 2;
                        P3 = 3;
                        P4 = 4;
                        P5 = 5;
                        P6 = 6;
                        P7 = 7;
                        P8 = 8;
                        P9 = 9;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2, c.P3, c.P4, c.P5, c.P6, c.P7, c.P9));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, "(1, 2, 3, 0, 0, 6, 0, 9)"));
            verifier.VerifyDiagnostics();
            if (typeKind == "class")
            {
                verifier.VerifyIL("C..ctor", $$"""
                    {
                      // Code size       71 (0x47)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  call       "object..ctor()"
                      IL_0006:  ldarg.0
                      IL_0007:  ldc.i4.1
                      IL_0008:  stfld      "int C.<P1>k__BackingField"
                      IL_000d:  ldarg.0
                      IL_000e:  ldc.i4.2
                      IL_000f:  stfld      "int C.<P2>k__BackingField"
                      IL_0014:  ldarg.0
                      IL_0015:  ldc.i4.3
                      IL_0016:  call       "void C.P3.{{setter}}"
                      IL_001b:  ldarg.0
                      IL_001c:  ldc.i4.4
                      IL_001d:  call       "void C.P4.{{setter}}"
                      IL_0022:  ldarg.0
                      IL_0023:  ldc.i4.5
                      IL_0024:  call       "void C.P5.{{setter}}"
                      IL_0029:  ldarg.0
                      IL_002a:  ldc.i4.6
                      IL_002b:  call       "void C.P6.{{setter}}"
                      IL_0030:  ldarg.0
                      IL_0031:  ldc.i4.7
                      IL_0032:  call       "void C.P7.{{setter}}"
                      IL_0037:  ldarg.0
                      IL_0038:  ldc.i4.8
                      IL_0039:  call       "void C.P8.{{setter}}"
                      IL_003e:  ldarg.0
                      IL_003f:  ldc.i4.s   9
                      IL_0041:  call       "void C.P9.{{setter}}"
                      IL_0046:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("C..ctor", $$"""
                    {
                      // Code size      121 (0x79)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int C.<P2>k__BackingField"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int C.<P3>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  ldc.i4.0
                      IL_0010:  stfld      "int C.<P4>k__BackingField"
                      IL_0015:  ldarg.0
                      IL_0016:  ldc.i4.0
                      IL_0017:  stfld      "int C.<P5>k__BackingField"
                      IL_001c:  ldarg.0
                      IL_001d:  ldc.i4.0
                      IL_001e:  stfld      "int C.<P6>k__BackingField"
                      IL_0023:  ldarg.0
                      IL_0024:  ldc.i4.0
                      IL_0025:  stfld      "int C.<P7>k__BackingField"
                      IL_002a:  ldarg.0
                      IL_002b:  ldc.i4.0
                      IL_002c:  stfld      "int C.<P8>k__BackingField"
                      IL_0031:  ldarg.0
                      IL_0032:  ldc.i4.0
                      IL_0033:  stfld      "int C.<P9>k__BackingField"
                      IL_0038:  ldarg.0
                      IL_0039:  ldc.i4.1
                      IL_003a:  stfld      "int C.<P1>k__BackingField"
                      IL_003f:  ldarg.0
                      IL_0040:  ldc.i4.2
                      IL_0041:  stfld      "int C.<P2>k__BackingField"
                      IL_0046:  ldarg.0
                      IL_0047:  ldc.i4.3
                      IL_0048:  call       "void C.P3.{{setter}}"
                      IL_004d:  ldarg.0
                      IL_004e:  ldc.i4.4
                      IL_004f:  call       "void C.P4.{{setter}}"
                      IL_0054:  ldarg.0
                      IL_0055:  ldc.i4.5
                      IL_0056:  call       "void C.P5.{{setter}}"
                      IL_005b:  ldarg.0
                      IL_005c:  ldc.i4.6
                      IL_005d:  call       "void C.P6.{{setter}}"
                      IL_0062:  ldarg.0
                      IL_0063:  ldc.i4.7
                      IL_0064:  call       "void C.P7.{{setter}}"
                      IL_0069:  ldarg.0
                      IL_006a:  ldc.i4.8
                      IL_006b:  call       "void C.P8.{{setter}}"
                      IL_0070:  ldarg.0
                      IL_0071:  ldc.i4.s   9
                      IL_0073:  call       "void C.P9.{{setter}}"
                      IL_0078:  ret
                    }
                    """);
            }
        }

        [Fact]
        public void ConstructorAssignment_03()
        {
            string source = """
                using System;
                class C
                {
                    static int P1 => field;
                    int P2 => field;
                    static C()
                    {
                        P1 = 1;
                        M(() => { P1 = 2; });
                    }
                    C(object o)
                    {
                        P2 = 3;
                        M(() => { P2 = 4; });
                    }
                    static void M(Action a)
                    {
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (9,19): error CS0200: Property or indexer 'C.P1' cannot be assigned to -- it is read only
                //         M(() => { P1 = 2; });
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P1").WithArguments("C.P1").WithLocation(9, 19),
                // (14,19): error CS0200: Property or indexer 'C.P2' cannot be assigned to -- it is read only
                //         M(() => { P2 = 4; });
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P2").WithArguments("C.P2").WithLocation(14, 19));
        }

        [Fact]
        public void ConstructorAssignment_04()
        {
            string source = """
                using System;
                class A
                {
                    public static int P1 { get; private set; }
                    public static int P3 { get => field; private set; }
                    public static int P5 { get => field; private set { } }
                    public static int P7 { get; private set { } }
                }
                class B : A
                {
                    static B()
                    {
                        P1 = 1;
                        P3 = 3;
                        P5 = 5;
                        P7 = 7;
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0272: The property or indexer 'A.P1' cannot be used in this context because the set accessor is inaccessible
                //         P1 = 1;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P1").WithArguments("A.P1").WithLocation(13, 9),
                // (14,9): error CS0272: The property or indexer 'A.P3' cannot be used in this context because the set accessor is inaccessible
                //         P3 = 3;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P3").WithArguments("A.P3").WithLocation(14, 9),
                // (15,9): error CS0272: The property or indexer 'A.P5' cannot be used in this context because the set accessor is inaccessible
                //         P5 = 5;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P5").WithArguments("A.P5").WithLocation(15, 9),
                // (16,9): error CS0272: The property or indexer 'A.P7' cannot be used in this context because the set accessor is inaccessible
                //         P7 = 7;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P7").WithArguments("A.P7").WithLocation(16, 9));
        }

        [Fact]
        public void ConstructorAssignment_05()
        {
            string source = """
                using System;
                class A
                {
                    public int P1 { get; private set; }
                    public int P2 { get; private init; }
                    public int P3 { get => field; private set; }
                    public int P4 { get => field; private init; }
                    public int P5 { get => field; private set { } }
                    public int P6 { get => field; private init { } }
                    public int P7 { get; private set { } }
                    public int P8 { get; private init { } }
                }
                class B : A
                {
                    public B()
                    {
                        P1 = 1;
                        P2 = 2;
                        P3 = 3;
                        P4 = 4;
                        P5 = 5;
                        P6 = 6;
                        P7 = 7;
                        P8 = 8;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (17,9): error CS0272: The property or indexer 'A.P1' cannot be used in this context because the set accessor is inaccessible
                //         P1 = 1;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P1").WithArguments("A.P1").WithLocation(17, 9),
                // (18,9): error CS0272: The property or indexer 'A.P2' cannot be used in this context because the set accessor is inaccessible
                //         P2 = 2;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P2").WithArguments("A.P2").WithLocation(18, 9),
                // (19,9): error CS0272: The property or indexer 'A.P3' cannot be used in this context because the set accessor is inaccessible
                //         P3 = 3;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P3").WithArguments("A.P3").WithLocation(19, 9),
                // (20,9): error CS0272: The property or indexer 'A.P4' cannot be used in this context because the set accessor is inaccessible
                //         P4 = 4;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P4").WithArguments("A.P4").WithLocation(20, 9),
                // (21,9): error CS0272: The property or indexer 'A.P5' cannot be used in this context because the set accessor is inaccessible
                //         P5 = 5;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P5").WithArguments("A.P5").WithLocation(21, 9),
                // (22,9): error CS0272: The property or indexer 'A.P6' cannot be used in this context because the set accessor is inaccessible
                //         P6 = 6;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P6").WithArguments("A.P6").WithLocation(22, 9),
                // (23,9): error CS0272: The property or indexer 'A.P7' cannot be used in this context because the set accessor is inaccessible
                //         P7 = 7;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P7").WithArguments("A.P7").WithLocation(23, 9),
                // (24,9): error CS0272: The property or indexer 'A.P8' cannot be used in this context because the set accessor is inaccessible
                //         P8 = 8;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "P8").WithArguments("A.P8").WithLocation(24, 9));
        }

        [Fact]
        public void ConstructorAssignment_06()
        {
            string source = $$"""
                class A
                {
                    public object P1 { get; }
                    public object P2 { get => field; }
                    public object P3 { get => field; init; }
                    public A()
                    {
                        this.P1 = 11;
                        this.P2 = 12;
                        this.P3 = 13;
                    }
                    A(A a)
                    {
                        a.P1 = 31;
                        a.P2 = 32;
                        a.P3 = 33;
                    }
                }
                class B : A
                {
                    B()
                    {
                        base.P1 = 21;
                        base.P2 = 22;
                        base.P3 = 23;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (14,9): error CS0200: Property or indexer 'A.P1' cannot be assigned to -- it is read only
                //         a.P1 = 31;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "a.P1").WithArguments("A.P1").WithLocation(14, 9),
                // (15,9): error CS0200: Property or indexer 'A.P2' cannot be assigned to -- it is read only
                //         a.P2 = 32;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "a.P2").WithArguments("A.P2").WithLocation(15, 9),
                // (16,9): error CS8852: Init-only property or indexer 'A.P3' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         a.P3 = 33;
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "a.P3").WithArguments("A.P3").WithLocation(16, 9),
                // (23,9): error CS0200: Property or indexer 'A.P1' cannot be assigned to -- it is read only
                //         base.P1 = 21;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "base.P1").WithArguments("A.P1").WithLocation(23, 9),
                // (24,9): error CS0200: Property or indexer 'A.P2' cannot be assigned to -- it is read only
                //         base.P2 = 22;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "base.P2").WithArguments("A.P2").WithLocation(24, 9));
        }

        [Fact]
        public void ConstructorAssignment_07()
        {
            string source = $$"""
                using System;
                class C
                {
                    public static int P1 { get; }
                    public static int P2 { get => field; }
                    public static int P3 { get => field; set; }
                    public static int P4 { get => field; set { } }
                    public static int P5 = F(
                        P1 = 1,
                        P2 = 2,
                        P3 = 3,
                        P4 = 4);
                    static int F(int x, int y, int z, int w) => x;
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P4));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2, 3, 0)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       39 (0x27)
                  .maxstack  5
                  IL_0000:  ldc.i4.1
                  IL_0001:  dup
                  IL_0002:  stsfld     "int C.<P1>k__BackingField"
                  IL_0007:  ldc.i4.2
                  IL_0008:  dup
                  IL_0009:  stsfld     "int C.<P2>k__BackingField"
                  IL_000e:  ldc.i4.3
                  IL_000f:  dup
                  IL_0010:  call       "void C.P3.set"
                  IL_0015:  ldc.i4.4
                  IL_0016:  dup
                  IL_0017:  call       "void C.P4.set"
                  IL_001c:  call       "int C.F(int, int, int, int)"
                  IL_0021:  stsfld     "int C.P5"
                  IL_0026:  ret
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        public void DefaultInitialization_01(bool useRefStruct, bool useInit, bool includeStructInitializationWarnings)
        {
            string typeKind = useRefStruct ? "ref struct" : "    struct";
            string setter = useInit ? "init" : "set";
            string source = $$"""
                using System;
                {{typeKind}} S0
                {
                    public int F0;
                    public int P0 { get; {{setter}}; }
                    public S0(int unused) { _ = P0; }
                }
                {{typeKind}} S1
                {
                    public int F1;
                    public int P1 { get => field; }
                    public S1(int unused) { _ = P1; }
                }
                {{typeKind}} S2
                {
                    public int F2;
                    public int P2 { get => field; {{setter}}; }
                    public S2(int unused) { _ = P2; }
                }
                {{typeKind}} S3
                {
                    public int F3;
                    public int P3 { get => field; {{setter}} { field = value; } }
                    public S3(int unused) { _ = P3; }
                }
                class Program
                {
                    static void Main()
                    {
                        var s0 = new S0(-1);
                        var s1 = new S1(1);
                        var s2 = new S2(2);
                        var s3 = new S3(3);
                        Console.WriteLine((s0.F0, s0.P0));
                        Console.WriteLine((s1.F1, s1.P1));
                        Console.WriteLine((s2.F2, s2.P2));
                        Console.WriteLine((s3.F3, s3.P3));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                options: includeStructInitializationWarnings ? TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings) : TestOptions.ReleaseExe,
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, """
                    (0, 0)
                    (0, 0)
                    (0, 0)
                    (0, 0)
                    """));
            if (includeStructInitializationWarnings)
            {
                verifier.VerifyDiagnostics(
                    // (4,16): warning CS0649: Field 'S0.F0' is never assigned to, and will always have its default value 0
                    //     public int F0;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F0").WithArguments("S0.F0", "0").WithLocation(4, 16),
                    // (6,12): warning CS9021: Control is returned to caller before auto-implemented property 'S0.P0' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S0(int unused) { _ = P0; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "S0").WithArguments("S0.P0").WithLocation(6, 12),
                    // (6,12): warning CS9022: Control is returned to caller before field 'S0.F0' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S0(int unused) { _ = P0; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S0").WithArguments("S0.F0").WithLocation(6, 12),
                    // (6,33): warning CS9018: Auto-implemented property 'P0' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S0(int unused) { _ = P0; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationPropertySupportedVersion, "P0").WithArguments("P0").WithLocation(6, 33),
                    // (10,16): warning CS0649: Field 'S1.F1' is never assigned to, and will always have its default value 0
                    //     public int F1;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("S1.F1", "0").WithLocation(10, 16),
                    // (12,33): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
                    //     public S1(int unused) { _ = P1; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P1").WithLocation(12, 33),
                    // (16,16): warning CS0649: Field 'S2.F2' is never assigned to, and will always have its default value 0
                    //     public int F2;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("S2.F2", "0").WithLocation(16, 16),
                    // (18,12): warning CS9021: Control is returned to caller before auto-implemented property 'S2.P2' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S2(int unused) { _ = P2; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "S2").WithArguments("S2.P2").WithLocation(18, 12),
                    // (18,12): warning CS9022: Control is returned to caller before field 'S2.F2' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S2(int unused) { _ = P2; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S2").WithArguments("S2.F2").WithLocation(18, 12),
                    // (18,33): warning CS9018: Auto-implemented property 'P2' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S2(int unused) { _ = P2; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationPropertySupportedVersion, "P2").WithArguments("P2").WithLocation(18, 33),
                    // (22,16): warning CS0649: Field 'S3.F3' is never assigned to, and will always have its default value 0
                    //     public int F3;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F3").WithArguments("S3.F3", "0").WithLocation(22, 16),
                    // (24,33): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
                    //     public S3(int unused) { _ = P3; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P3").WithLocation(24, 33));
            }
            else
            {
                verifier.VerifyDiagnostics(
                    // (4,16): warning CS0649: Field 'S0.F0' is never assigned to, and will always have its default value 0
                    //     public int F0;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F0").WithArguments("S0.F0", "0").WithLocation(4, 16),
                    // (10,16): warning CS0649: Field 'S1.F1' is never assigned to, and will always have its default value 0
                    //     public int F1;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("S1.F1", "0").WithLocation(10, 16),
                    // (16,16): warning CS0649: Field 'S2.F2' is never assigned to, and will always have its default value 0
                    //     public int F2;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("S2.F2", "0").WithLocation(16, 16),
                    // (22,16): warning CS0649: Field 'S3.F3' is never assigned to, and will always have its default value 0
                    //     public int F3;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F3").WithArguments("S3.F3", "0").WithLocation(22, 16));
            }
            verifier.VerifyIL("S0..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S0.F0"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S0.<P0>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  call       "readonly int S0.P0.get"
                      IL_0014:  pop
                      IL_0015:  ret
                    }
                    """);
            verifier.VerifyIL("S1..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S1.F1"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S1.<P1>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  call       "int S1.P1.get"
                      IL_0014:  pop
                      IL_0015:  ret
                    }
                    """);
            verifier.VerifyIL("S2..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S2.F2"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S2.<P2>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  call       "int S2.P2.get"
                      IL_0014:  pop
                      IL_0015:  ret
                    }
                    """);
            verifier.VerifyIL("S3..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S3.F3"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S3.<P3>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  call       "int S3.P3.get"
                      IL_0014:  pop
                      IL_0015:  ret
                    }
                    """);
        }

        [Theory]
        [CombinatorialData]
        public void DefaultInitialization_02(bool useRefStruct, bool useInit, bool includeStructInitializationWarnings)
        {
            string typeKind = useRefStruct ? "ref struct" : "    struct";
            string setter = useInit ? "init" : "set";
            string source = $$"""
                using System;
                {{typeKind}} S0
                {
                    public int F0;
                    public int P0 { get; {{setter}}; }
                    public S0(int i) { P0 = i; }
                }
                {{typeKind}} S1
                {
                    public int F1;
                    public int P1 { get => field; }
                    public S1(int i) { P1 = i; }
                }
                {{typeKind}} S2
                {
                    public int F2;
                    public int P2 { get => field; {{setter}}; }
                    public S2(int i) { P2 = i; }
                }
                {{typeKind}} S3
                {
                    public int F3;
                    public int P3 { get => field; {{setter}} { field = value; } }
                    public S3(int i) { P3 = i; }
                }
                class Program
                {
                    static void Main()
                    {
                        var s0 = new S0(-1);
                        var s1 = new S1(1);
                        var s2 = new S2(2);
                        var s3 = new S3(3);
                        Console.WriteLine((s0.F0, s0.P0));
                        Console.WriteLine((s1.F1, s1.P1));
                        Console.WriteLine((s2.F2, s2.P2));
                        Console.WriteLine((s3.F3, s3.P3));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                source,
                options: includeStructInitializationWarnings ? TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings) : TestOptions.ReleaseExe,
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, """
                    (0, -1)
                    (0, 1)
                    (0, 2)
                    (0, 3)
                    """));
            if (includeStructInitializationWarnings)
            {
                verifier.VerifyDiagnostics(
                    // (4,16): warning CS0649: Field 'S0.F0' is never assigned to, and will always have its default value 0
                    //     public int F0;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F0").WithArguments("S0.F0", "0").WithLocation(4, 16),
                    // (6,12): warning CS9022: Control is returned to caller before field 'S0.F0' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S0(int i) { P0 = i; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S0").WithArguments("S0.F0").WithLocation(6, 12),
                    // (10,16): warning CS0649: Field 'S1.F1' is never assigned to, and will always have its default value 0
                    //     public int F1;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("S1.F1", "0").WithLocation(10, 16),
                    // (12,24): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
                    //     public S1(int i) { P1 = i; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P1").WithLocation(12, 24),
                    // (16,16): warning CS0649: Field 'S2.F2' is never assigned to, and will always have its default value 0
                    //     public int F2;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("S2.F2", "0").WithLocation(16, 16),
                    // (18,12): warning CS9022: Control is returned to caller before field 'S2.F2' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                    //     public S2(int i) { P2 = i; }
                    Diagnostic(ErrorCode.WRN_UnassignedThisSupportedVersion, "S2").WithArguments("S2.F2").WithLocation(18, 12),
                    // (22,16): warning CS0649: Field 'S3.F3' is never assigned to, and will always have its default value 0
                    //     public int F3;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F3").WithArguments("S3.F3", "0").WithLocation(22, 16),
                    // (24,24): warning CS9020: The 'this' object is read before all of its fields have been assigned, causing preceding implicit assignments of 'default' to non-explicitly assigned fields.
                    //     public S3(int i) { P3 = i; }
                    Diagnostic(ErrorCode.WRN_UseDefViolationThisSupportedVersion, "P3").WithLocation(24, 24));
            }
            else
            {
                verifier.VerifyDiagnostics(
                    // (4,16): warning CS0649: Field 'S0.F0' is never assigned to, and will always have its default value 0
                    //     public int F0;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F0").WithArguments("S0.F0", "0").WithLocation(4, 16),
                    // (10,16): warning CS0649: Field 'S1.F1' is never assigned to, and will always have its default value 0
                    //     public int F1;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F1").WithArguments("S1.F1", "0").WithLocation(10, 16),
                    // (16,16): warning CS0649: Field 'S2.F2' is never assigned to, and will always have its default value 0
                    //     public int F2;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F2").WithArguments("S2.F2", "0").WithLocation(16, 16),
                    // (22,16): warning CS0649: Field 'S3.F3' is never assigned to, and will always have its default value 0
                    //     public int F3;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F3").WithArguments("S3.F3", "0").WithLocation(22, 16));
            }
            verifier.VerifyIL("S0..ctor", $$"""
                    {
                      // Code size       15 (0xf)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S0.F0"
                      IL_0007:  ldarg.0
                      IL_0008:  ldarg.1
                      IL_0009:  call       "void S0.P0.{{setter}}"
                      IL_000e:  ret
                    }
                    """);
            verifier.VerifyIL("S1..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S1.F1"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S1.<P1>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  ldarg.1
                      IL_0010:  stfld      "int S1.<P1>k__BackingField"
                      IL_0015:  ret
                    }
                    """);
            verifier.VerifyIL("S2..ctor", $$"""
                    {
                      // Code size       15 (0xf)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S2.F2"
                      IL_0007:  ldarg.0
                      IL_0008:  ldarg.1
                      IL_0009:  call       "void S2.P2.{{setter}}"
                      IL_000e:  ret
                    }
                    """);
            verifier.VerifyIL("S3..ctor", $$"""
                    {
                      // Code size       22 (0x16)
                      .maxstack  2
                      IL_0000:  ldarg.0
                      IL_0001:  ldc.i4.0
                      IL_0002:  stfld      "int S3.F3"
                      IL_0007:  ldarg.0
                      IL_0008:  ldc.i4.0
                      IL_0009:  stfld      "int S3.<P3>k__BackingField"
                      IL_000e:  ldarg.0
                      IL_000f:  ldarg.1
                      IL_0010:  call       "void S3.P3.{{setter}}"
                      IL_0015:  ret
                    }
                    """);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_01(bool useReadOnlyType, bool useReadOnlyProperty, bool useInit)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string propertyModifier = getReadOnlyModifier(useReadOnlyProperty);
            string setter = useInit ? "init" : "set";
            string source = $$"""
                {{typeModifier}} struct S
                {
                    {{propertyModifier}} object P1 { get; }
                    {{propertyModifier}} object P2 { get => field; }
                    {{propertyModifier}} object P3 { get => field; {{setter}}; }
                    {{propertyModifier}} object P4 { get => field; {{setter}} { } }
                    {{propertyModifier}} object P5 { get => null; }
                    {{propertyModifier}} object P6 { get => null; {{setter}}; }
                    {{propertyModifier}} object P7 { get => null; {{setter}} { } }
                    {{propertyModifier}} object P8 { get => null; {{setter}} { _ = field; } }
                    {{propertyModifier}} object P9 { get; {{setter}}; }
                    {{propertyModifier}} object PA { get; {{setter}} { } }
                    {{propertyModifier}} object PB { {{setter}} { _ = field; } }
                    {{propertyModifier}} object PC { get; {{setter}} { field = value; } }
                    {{propertyModifier}} object PD { {{setter}} { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            if (useInit)
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (useReadOnlyType)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object P3 { get => field; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P3").WithLocation(5, 21),
                    // (8,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object P6 { get => null; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P6").WithLocation(8, 21),
                    // (11,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object P9 { get; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P9").WithLocation(11, 21),
                    // (14,37): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                    //              object PC { get; set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(14, 37),
                    // (15,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                    //              object PD { set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(15, 32));
            }
            else if (useReadOnlyProperty)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,21): error CS8659: Auto-implemented property 'S.P3' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object P3 { get => field; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P3").WithArguments("S.P3").WithLocation(5, 21),
                    // (8,21): error CS8659: Auto-implemented property 'S.P6' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object P6 { get => null; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P6").WithArguments("S.P6").WithLocation(8, 21),
                    // (11,21): error CS8659: Auto-implemented property 'S.P9' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object P9 { get; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P9").WithArguments("S.P9").WithLocation(11, 21),
                    // (14,37): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                    //     readonly object PC { get; set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(14, 37),
                    // (15,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                    //     readonly object PD { set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(15, 32));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object S.<P1>k__BackingField: True",
                $"System.Object S.<P2>k__BackingField: {useReadOnlyType || useReadOnlyProperty}",
                $"System.Object S.<P3>k__BackingField: {useReadOnlyType || useReadOnlyProperty}",
                $"System.Object S.<P4>k__BackingField: {useReadOnlyType || useReadOnlyProperty}",
                $"System.Object S.<P6>k__BackingField: {useReadOnlyType || useReadOnlyProperty}",
                $"System.Object S.<P8>k__BackingField: {useReadOnlyType || useReadOnlyProperty}",
                $"System.Object S.<P9>k__BackingField: {useReadOnlyType || useReadOnlyProperty || useInit}",
                $"System.Object S.<PA>k__BackingField: {useReadOnlyType || useReadOnlyProperty || useInit}",
                $"System.Object S.<PB>k__BackingField: {useReadOnlyType || useReadOnlyProperty || useInit}",
                $"System.Object S.<PC>k__BackingField: {useReadOnlyType || useReadOnlyProperty || useInit}",
                $"System.Object S.<PD>k__BackingField: {useReadOnlyType || useReadOnlyProperty || useInit}",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_02(bool useReadOnlyType, bool useReadOnlyOnGet)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string getModifier = getReadOnlyModifier(useReadOnlyOnGet);
            string setModifier = getReadOnlyModifier(!useReadOnlyOnGet);
            string source = $$"""
                {{typeModifier}} struct S
                {
                    object P3 { {{getModifier}} get => field; {{setModifier}} set; }
                    object P4 { {{getModifier}} get => field; {{setModifier}} set { } }
                    object P6 { {{getModifier}} get => null; {{setModifier}} set; }
                    object P7 { {{getModifier}} get => null; {{setModifier}} set { } }
                    object P8 { {{getModifier}} get => null; {{setModifier}} set { _ = field; } }
                    object P9 { {{getModifier}} get; {{setModifier}} set; }
                    object PA { {{getModifier}} get; {{setModifier}} set { } }
                    object PC { {{getModifier}} get; {{setModifier}} set { field = value; } }
                }
                """;
            var comp = CreateCompilation(source);
            if (useReadOnlyType)
            {
                if (useReadOnlyOnGet)
                {
                    comp.VerifyEmitDiagnostics(
                        // (3,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P3 { readonly get => field;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P3").WithLocation(3, 12),
                        // (5,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P6 { readonly get => null;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P6").WithLocation(5, 12),
                        // (8,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P9 { readonly get;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P9").WithLocation(8, 12),
                        // (10,46): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                        //     object PC { readonly get;          set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(10, 46));
                }
                else
                {
                    comp.VerifyEmitDiagnostics(
                        // (3,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P3 {          get => field; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P3").WithLocation(3, 12),
                        // (3,49): error CS8658: Auto-implemented 'set' accessor 'S.P3.set' cannot be marked 'readonly'.
                        //     object P3 {          get => field; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P3.set").WithLocation(3, 49),
                        // (5,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P6 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P6").WithLocation(5, 12),
                        // (5,48): error CS8658: Auto-implemented 'set' accessor 'S.P6.set' cannot be marked 'readonly'.
                        //     object P6 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P6.set").WithLocation(5, 48),
                        // (8,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P9 {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P9").WithLocation(8, 12),
                        // (8,40): error CS8658: Auto-implemented 'set' accessor 'S.P9.set' cannot be marked 'readonly'.
                        //     object P9 {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P9.set").WithLocation(8, 40),
                        // (10,46): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                        //     object PC {          get; readonly set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(10, 46));
                }
            }
            else
            {
                if (useReadOnlyOnGet)
                {
                    comp.VerifyEmitDiagnostics();
                }
                else
                {
                    comp.VerifyEmitDiagnostics(
                        // (3,49): error CS8658: Auto-implemented 'set' accessor 'S.P3.set' cannot be marked 'readonly'.
                        //     object P3 {          get => field; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P3.set").WithLocation(3, 49),
                        // (5,48): error CS8658: Auto-implemented 'set' accessor 'S.P6.set' cannot be marked 'readonly'.
                        //     object P6 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P6.set").WithLocation(5, 48),
                        // (8,40): error CS8658: Auto-implemented 'set' accessor 'S.P9.set' cannot be marked 'readonly'.
                        //     object P9 {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P9.set").WithLocation(8, 40),
                        // (10,46): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                        //     object PC {          get; readonly set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(10, 46));
                }
            }
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object S.<P3>k__BackingField: {useReadOnlyType}",
                $"System.Object S.<P4>k__BackingField: {useReadOnlyType}",
                $"System.Object S.<P6>k__BackingField: {useReadOnlyType}",
                $"System.Object S.<P8>k__BackingField: {useReadOnlyType}",
                $"System.Object S.<P9>k__BackingField: {useReadOnlyType || !useReadOnlyOnGet}",
                $"System.Object S.<PA>k__BackingField: {useReadOnlyType || !useReadOnlyOnGet}",
                $"System.Object S.<PC>k__BackingField: {useReadOnlyType || !useReadOnlyOnGet}",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_03(bool useRefStruct, bool useReadOnlyType, bool useReadOnlyMember)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeKind = useRefStruct ? "ref struct" : "    struct";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string memberModifier = getReadOnlyModifier(useReadOnlyMember);
            string sourceA = $$"""
                {{typeModifier}} {{typeKind}} S
                {
                    {{memberModifier}} object P1 { get; }
                    {{memberModifier}} object P5 { get; init; }
                    object P7 { {{memberModifier}} get; init; }
                    {{memberModifier}} object Q1 { get => field; }
                    {{memberModifier}} object Q2 { set { _ = field; } }
                    {{memberModifier}} object Q3 { init { _ = field; } }
                    {{memberModifier}} object Q4 { get; set { _ = field; } }
                    {{memberModifier}} object Q5 { get; init { _ = field; } }
                    object Q6 { {{memberModifier}} get; set { _ = field; } }
                    object Q7 { {{memberModifier}} get; init { _ = field; } }
                    object Q8 { get; {{memberModifier}} set { _ = field; } }
                }
                """;
            string sourceB = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(S).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput($$"""
                <P1>k__BackingField: True
                <P5>k__BackingField: True
                <P7>k__BackingField: True
                <Q1>k__BackingField: {{useReadOnlyType || useReadOnlyMember}}
                <Q2>k__BackingField: {{useReadOnlyType || useReadOnlyMember}}
                <Q3>k__BackingField: True
                <Q4>k__BackingField: {{useReadOnlyType || useReadOnlyMember}}
                <Q5>k__BackingField: True
                <Q6>k__BackingField: {{useReadOnlyType}}
                <Q7>k__BackingField: True
                <Q8>k__BackingField: {{useReadOnlyType || useReadOnlyMember}}
                """));
            var comp = (CSharpCompilation)verifier.Compilation;
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object S.<P1>k__BackingField: True",
                $"System.Object S.<P5>k__BackingField: True",
                $"System.Object S.<P7>k__BackingField: True",
                $"System.Object S.<Q1>k__BackingField: {useReadOnlyType || useReadOnlyMember}",
                $"System.Object S.<Q2>k__BackingField: {useReadOnlyType || useReadOnlyMember}",
                $"System.Object S.<Q3>k__BackingField: True",
                $"System.Object S.<Q4>k__BackingField: {useReadOnlyType || useReadOnlyMember}",
                $"System.Object S.<Q5>k__BackingField: True",
                $"System.Object S.<Q6>k__BackingField: {useReadOnlyType}",
                $"System.Object S.<Q7>k__BackingField: True",
                $"System.Object S.<Q8>k__BackingField: {useReadOnlyType || useReadOnlyMember}",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_04(bool useRefStruct, bool useReadOnlyType, bool useReadOnlyMember)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeKind = useRefStruct ? "ref struct" : "    struct";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string memberModifier = getReadOnlyModifier(useReadOnlyMember);
            string sourceA = $$"""
                {{typeModifier}} {{typeKind}} S
                {
                    static {{memberModifier}} object P1 { get; }
                    static {{memberModifier}} object Q1 { get => field; }
                    static {{memberModifier}} object Q2 { set { _ = field; } }
                    static {{memberModifier}} object Q4 { get; set { _ = field; } }
                    static object Q6 { {{memberModifier}} get; set { _ = field; } }
                    static object Q8 { get; {{memberModifier}} set { _ = field; } }
                }
                """;
            string sourceB = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(S).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            var comp = CreateCompilation([sourceA, sourceB], options: TestOptions.ReleaseExe);
            if (useReadOnlyMember)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,28): error CS8657: Static member 'S.P1' cannot be marked 'readonly'.
                    //     static readonly object P1 { get; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P1").WithArguments("S.P1").WithLocation(3, 28),
                    // (4,28): error CS8657: Static member 'S.Q1' cannot be marked 'readonly'.
                    //     static readonly object Q1 { get => field; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "Q1").WithArguments("S.Q1").WithLocation(4, 28),
                    // (5,28): error CS8657: Static member 'S.Q2' cannot be marked 'readonly'.
                    //     static readonly object Q2 { set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "Q2").WithArguments("S.Q2").WithLocation(5, 28),
                    // (6,28): error CS8657: Static member 'S.Q4' cannot be marked 'readonly'.
                    //     static readonly object Q4 { get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "Q4").WithArguments("S.Q4").WithLocation(6, 28),
                    // (7,33): error CS8657: Static member 'S.Q6.get' cannot be marked 'readonly'.
                    //     static object Q6 { readonly get; set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.Q6.get").WithLocation(7, 33),
                    // (8,38): error CS8657: Static member 'S.Q8.set' cannot be marked 'readonly'.
                    //     static object Q8 { get; readonly set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "set").WithArguments("S.Q8.set").WithLocation(8, 38));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
                var verifier = CompileAndVerify(comp, expectedOutput: $$"""
                    <P1>k__BackingField: True
                    <Q1>k__BackingField: {{useReadOnlyMember}}
                    <Q2>k__BackingField: {{useReadOnlyMember}}
                    <Q4>k__BackingField: {{useReadOnlyMember}}
                    <Q6>k__BackingField: False
                    <Q8>k__BackingField: {{useReadOnlyMember}}
                    """);
            }
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object S.<P1>k__BackingField: True",
                $"System.Object S.<Q1>k__BackingField: {useReadOnlyMember}",
                $"System.Object S.<Q2>k__BackingField: {useReadOnlyMember}",
                $"System.Object S.<Q4>k__BackingField: {useReadOnlyMember}",
                $"System.Object S.<Q6>k__BackingField: False",
                $"System.Object S.<Q8>k__BackingField: {useReadOnlyMember}",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_05(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                struct S
                {
                    object P1 { readonly get; }
                    object P2 { readonly {{setter}}; }
                    object P3 { readonly get; {{setter}}; }
                    object P4 { get; readonly {{setter}}; }
                    object P5 { readonly get; readonly {{setter}}; }
                    object Q1 { readonly get => field; }
                    object Q2 { readonly {{setter}} { _ = field; } }
                    object Q3 { readonly get => field; {{setter}}; }
                    object Q4 { get; readonly {{setter}} { } }
                    object Q5 { readonly get => field; readonly {{setter}} { } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            if (useInit)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,12): error CS8664: 'S.P1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object P1 { readonly get; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P1").WithArguments("S.P1").WithLocation(3, 12),
                    // (4,12): error CS8664: 'S.P2': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object P2 { readonly init; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P2").WithArguments("S.P2").WithLocation(4, 12),
                    // (4,26): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.P2' readonly instead.
                    //     object P2 { readonly init; }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.P2").WithLocation(4, 26),
                    // (4,26): error CS8051: Auto-implemented properties must have get accessors.
                    //     object P2 { readonly init; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "init").WithLocation(4, 26),
                    // (6,31): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.P4' readonly instead.
                    //     object P4 { get; readonly init; }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.P4").WithLocation(6, 31),
                    // (7,12): error CS8661: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S.P5'. Instead, put a 'readonly' modifier on the property itself.
                    //     object P5 { readonly get; readonly init; }
                    Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "P5").WithArguments("S.P5").WithLocation(7, 12),
                    // (7,40): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.P5' readonly instead.
                    //     object P5 { readonly get; readonly init; }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.P5").WithLocation(7, 40),
                    // (8,12): error CS8664: 'S.Q1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object Q1 { readonly get => field; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "Q1").WithArguments("S.Q1").WithLocation(8, 12),
                    // (9,12): error CS8664: 'S.Q2': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object Q2 { readonly init { _ = field; } }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "Q2").WithArguments("S.Q2").WithLocation(9, 12),
                    // (9,26): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.Q2' readonly instead.
                    //     object Q2 { readonly init { _ = field; } }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.Q2").WithLocation(9, 26),
                    // (11,31): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.Q4' readonly instead.
                    //     object Q4 { get; readonly init { } }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.Q4").WithLocation(11, 31),
                    // (12,12): error CS8661: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S.Q5'. Instead, put a 'readonly' modifier on the property itself.
                    //     object Q5 { readonly get => field; readonly init { } }
                    Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "Q5").WithArguments("S.Q5").WithLocation(12, 12),
                    // (12,49): error CS8903: 'init' accessors cannot be marked 'readonly'. Mark 'S.Q5' readonly instead.
                    //     object Q5 { readonly get => field; readonly init { } }
                    Diagnostic(ErrorCode.ERR_InitCannotBeReadonly, "init").WithArguments("S.Q5").WithLocation(12, 49));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,12): error CS8664: 'S.P1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object P1 { readonly get; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P1").WithArguments("S.P1").WithLocation(3, 12),
                    // (4,12): error CS8664: 'S.P2': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object P2 { readonly set; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P2").WithArguments("S.P2").WithLocation(4, 12),
                    // (4,26): error CS8658: Auto-implemented 'set' accessor 'S.P2.set' cannot be marked 'readonly'.
                    //     object P2 { readonly set; }
                    Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P2.set").WithLocation(4, 26),
                    // (4,26): error CS8051: Auto-implemented properties must have get accessors.
                    //     object P2 { readonly set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithLocation(4, 26),
                    // (6,31): error CS8658: Auto-implemented 'set' accessor 'S.P4.set' cannot be marked 'readonly'.
                    //     object P4 { get; readonly set; }
                    Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P4.set").WithLocation(6, 31),
                    // (7,12): error CS8661: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S.P5'. Instead, put a 'readonly' modifier on the property itself.
                    //     object P5 { readonly get; readonly set; }
                    Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "P5").WithArguments("S.P5").WithLocation(7, 12),
                    // (7,40): error CS8658: Auto-implemented 'set' accessor 'S.P5.set' cannot be marked 'readonly'.
                    //     object P5 { readonly get; readonly set; }
                    Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P5.set").WithLocation(7, 40),
                    // (8,12): error CS8664: 'S.Q1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object Q1 { readonly get => field; }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "Q1").WithArguments("S.Q1").WithLocation(8, 12),
                    // (9,12): error CS8664: 'S.Q2': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                    //     object Q2 { readonly set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "Q2").WithArguments("S.Q2").WithLocation(9, 12),
                    // (12,12): error CS8661: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S.Q5'. Instead, put a 'readonly' modifier on the property itself.
                    //     object Q5 { readonly get => field; readonly set { } }
                    Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "Q5").WithArguments("S.Q5").WithLocation(12, 12));
            }
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object S.<P1>k__BackingField: True",
                $"System.Object S.<P2>k__BackingField: True",
                $"System.Object S.<P3>k__BackingField: {useInit}",
                $"System.Object S.<P4>k__BackingField: True",
                $"System.Object S.<P5>k__BackingField: True",
                $"System.Object S.<Q1>k__BackingField: True",
                $"System.Object S.<Q2>k__BackingField: True",
                $"System.Object S.<Q3>k__BackingField: {useInit}",
                $"System.Object S.<Q4>k__BackingField: True",
                $"System.Object S.<Q5>k__BackingField: True",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_06(bool useStatic)
        {
            string propertyModifier = useStatic ? "static" : "      ";
            string source = $$"""
                readonly class C1
                {
                    {{propertyModifier}} object P1 { get; }
                    {{propertyModifier}} object P2 { get; set; }
                    {{propertyModifier}} object P3 { get => field; }
                    {{propertyModifier}} object P4 { set { field = value; } }
                }
                class C2
                {
                    {{propertyModifier}} readonly object P1 { get; }
                    {{propertyModifier}} readonly object P2 { get; set; }
                    {{propertyModifier}} readonly object P3 { get => field; }
                    {{propertyModifier}} readonly object P4 { set { field = value; } }
                    {{propertyModifier}} object P5 { readonly get; set { field = value; } }
                    {{propertyModifier}} object P6 { get; readonly set { field = value; } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,16): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly class C1
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C1").WithArguments("readonly").WithLocation(1, 16),
                // (10,28): error CS0106: The modifier 'readonly' is not valid for this item
                //            readonly object P1 { get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("readonly").WithLocation(10, 28),
                // (11,28): error CS0106: The modifier 'readonly' is not valid for this item
                //            readonly object P2 { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P2").WithArguments("readonly").WithLocation(11, 28),
                // (12,28): error CS0106: The modifier 'readonly' is not valid for this item
                //            readonly object P3 { get => field; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P3").WithArguments("readonly").WithLocation(12, 28),
                // (13,28): error CS0106: The modifier 'readonly' is not valid for this item
                //            readonly object P4 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P4").WithArguments("readonly").WithLocation(13, 28),
                // (14,33): error CS0106: The modifier 'readonly' is not valid for this item
                //            object P5 { readonly get; set { field = value; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("readonly").WithLocation(14, 33),
                // (15,38): error CS0106: The modifier 'readonly' is not valid for this item
                //            object P6 { get; readonly set { field = value; } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("readonly").WithLocation(15, 38));
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C1").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            var expectedMembers = new[]
            {
                $"System.Object C1.<P1>k__BackingField: True",
                $"System.Object C1.<P2>k__BackingField: False",
                $"System.Object C1.<P3>k__BackingField: False",
                $"System.Object C1.<P4>k__BackingField: False",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
            actualMembers = comp.GetMember<NamedTypeSymbol>("C2").GetMembers().OfType<FieldSymbol>().Select(f => $"{f.ToTestDisplayString()}: {f.IsReadOnly}");
            expectedMembers = new[]
            {
                $"System.Object C2.<P1>k__BackingField: True",
                $"System.Object C2.<P2>k__BackingField: False",
                $"System.Object C2.<P3>k__BackingField: False",
                $"System.Object C2.<P4>k__BackingField: False",
                $"System.Object C2.<P5>k__BackingField: False",
                $"System.Object C2.<P6>k__BackingField: False",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void ReadOnly_07()
        {
            string source = """
                struct S0
                {
                    object P0 { get { field = null; return null; } }
                }
                struct S1
                {
                    object P1 { readonly get { field = null; return null; } }
                }
                struct S2
                {
                    readonly object P2 { get { field = null; return null; } }
                }
                readonly struct S3
                {
                    object P3 { get { field = null; return null; } }
                }
                readonly struct S4
                {
                    object P4 { readonly get { field = null; return null; } }
                }
                readonly struct S5
                {
                    readonly object P5 { get { field = null; return null; } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,12): error CS8664: 'S1.P1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                //     object P1 { readonly get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P1").WithArguments("S1.P1").WithLocation(7, 12),
                // (7,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     object P1 { readonly get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(7, 32),
                // (11,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     readonly object P2 { get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(11, 32),
                // (15,23): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     object P3 { get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(15, 23),
                // (19,12): error CS8664: 'S4.P4': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                //     object P4 { readonly get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P4").WithArguments("S4.P4").WithLocation(19, 12),
                // (19,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     object P4 { readonly get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(19, 32),
                // (23,32): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     readonly object P5 { get { field = null; return null; } }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(23, 32));
        }

        [Theory]
        [CombinatorialData]
        public void RefReturning_01(bool useStruct, bool useRefReadOnly)
        {
            string type = useStruct ? "struct" : "class";
            string refModifier = useRefReadOnly ? "ref readonly" : "ref         ";
            string source = $$"""
                {{type}} S
                {
                    {{refModifier}} object P1 { get; }
                    {{refModifier}} object P2 { get => ref field; }
                    {{refModifier}} object P3 { get => ref field; set; }
                    {{refModifier}} object P4 { get => ref field; init; }
                    {{refModifier}} object P5 { get => ref field; set { } }
                    {{refModifier}} object P6 { get => ref field; init { } }
                    {{refModifier}} object P7 { get => throw null; }
                    {{refModifier}} object P8 { get => throw null; set; }
                    {{refModifier}} object P9 { get => throw null; init; }
                    {{refModifier}} object PC { get => throw null; set { _ = field; } }
                    {{refModifier}} object PD { get => throw null; init { _ = field; } }
                    {{refModifier}} object PE { get; set; }
                    {{refModifier}} object PF { get; init; }
                    {{refModifier}} object PG { get; set { } }
                    {{refModifier}} object PH { get; init { } }
                    {{refModifier}} object PI { set { _ = field; } }
                    {{refModifier}} object PJ { init { _ = field; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P1 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P1").WithLocation(3, 25),
                // (4,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P2 { get => ref field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P2").WithLocation(4, 25),
                // (5,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P3 { get => ref field; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P3").WithLocation(5, 25),
                // (5,48): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P3 { get => ref field; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(5, 48),
                // (6,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P4 { get => ref field; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P4").WithLocation(6, 25),
                // (6,48): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P4 { get => ref field; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(6, 48),
                // (7,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P5 { get => ref field; set { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P5").WithLocation(7, 25),
                // (7,48): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P5 { get => ref field; set { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(7, 48),
                // (8,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P6 { get => ref field; init { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P6").WithLocation(8, 25),
                // (8,48): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P6 { get => ref field; init { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(8, 48),
                // (10,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P8 { get => throw null; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P8").WithLocation(10, 25),
                // (10,49): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P8 { get => throw null; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(10, 49),
                // (11,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object P9 { get => throw null; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P9").WithLocation(11, 25),
                // (11,49): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object P9 { get => throw null; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(11, 49),
                // (12,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PC { get => throw null; set { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PC").WithLocation(12, 25),
                // (12,49): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PC { get => throw null; set { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(12, 49),
                // (13,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PD { get => throw null; init { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PD").WithLocation(13, 25),
                // (13,49): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PD { get => throw null; init { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(13, 49),
                // (14,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PE { get; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PE").WithLocation(14, 25),
                // (14,35): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PE { get; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(14, 35),
                // (15,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PF { get; init; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PF").WithLocation(15, 25),
                // (15,35): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PF { get; init; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(15, 35),
                // (16,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PG { get; set { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PG").WithLocation(16, 25),
                // (16,35): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PG { get; set { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(16, 35),
                // (17,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PH { get; init { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PH").WithLocation(17, 25),
                // (17,35): error CS8147: Properties which return by reference cannot have set accessors
                //     ref          object PH { get; init { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "init").WithLocation(17, 35),
                // (18,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PI { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PI").WithLocation(18, 25),
                // (18,25): error CS8146: Properties which return by reference must have a get accessor
                //     ref          object PI { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "PI").WithLocation(18, 25),
                // (19,25): error CS8145: Auto-implemented properties cannot return by reference
                //     ref          object PJ { init { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PJ").WithLocation(19, 25),
                // (19,25): error CS8146: Properties which return by reference must have a get accessor
                //     ref          object PJ { init { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "PJ").WithLocation(19, 25));
        }

        [Theory]
        [CombinatorialData]
        public void RefReturning_02(bool useStruct, bool useRefReadOnly)
        {
            string type = useStruct ? "struct" : "class";
            string refModifier = useRefReadOnly ? "ref readonly" : "ref         ";
            string source = $$"""
                {{type}} S
                {
                    static {{refModifier}} object P1 { get; }
                    static {{refModifier}} object P2 { get => ref field; }
                    static {{refModifier}} object P3 { get => ref field; set; }
                    static {{refModifier}} object P5 { get => ref field; set { } }
                    static {{refModifier}} object P7 { get => throw null; }
                    static {{refModifier}} object P8 { get => throw null; set; }
                    static {{refModifier}} object PC { get => throw null; set { _ = field; } }
                    static {{refModifier}} object PE { get; set; }
                    static {{refModifier}} object PG { get; set { } }
                    static {{refModifier}} object PI { set { _ = field; } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object P1 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P1").WithLocation(3, 32),
                // (4,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object P2 { get => ref field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P2").WithLocation(4, 32),
                // (5,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object P3 { get => ref field; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P3").WithLocation(5, 32),
                // (5,55): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object P3 { get => ref field; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(5, 55),
                // (6,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object P5 { get => ref field; set { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P5").WithLocation(6, 32),
                // (6,55): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object P5 { get => ref field; set { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(6, 55),
                // (8,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object P8 { get => throw null; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P8").WithLocation(8, 32),
                // (8,56): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object P8 { get => throw null; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(8, 56),
                // (9,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object PC { get => throw null; set { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PC").WithLocation(9, 32),
                // (9,56): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object PC { get => throw null; set { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(9, 56),
                // (10,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object PE { get; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PE").WithLocation(10, 32),
                // (10,42): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object PE { get; set; }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(10, 42),
                // (11,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object PG { get; set { } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PG").WithLocation(11, 32),
                // (11,42): error CS8147: Properties which return by reference cannot have set accessors
                //     static ref          object PG { get; set { } }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithLocation(11, 42),
                // (12,32): error CS8145: Auto-implemented properties cannot return by reference
                //     static ref          object PI { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "PI").WithLocation(12, 32),
                // (12,32): error CS8146: Properties which return by reference must have a get accessor
                //     static ref          object PI { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, "PI").WithLocation(12, 32));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void AutoPropertyMustHaveGetAccessor(bool useStatic, bool useInit)
        {
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string source = $$"""
                class C
                {
                    {{modifier}} object P02 { {{setter}}; }
                    {{modifier}} object P03 { {{setter}} { } }
                    {{modifier}} object P04 { {{setter}} { field = value; } }
                    {{modifier}} object P11 { get; }
                    {{modifier}} object P12 { get; {{setter}}; }
                    {{modifier}} object P13 { get; {{setter}} { } }
                    {{modifier}} object P14 { get; {{setter}} { field = value; } }
                    {{modifier}} object P21 { get => field; }
                    {{modifier}} object P22 { get => field; {{setter}}; }
                    {{modifier}} object P23 { get => field; {{setter}} { } }
                    {{modifier}} object P24 { get => field; {{setter}} { field = value; } }
                    {{modifier}} object P31 { get => null; }
                    {{modifier}} object P32 { get => null; {{setter}}; }
                    {{modifier}} object P33 { get => null; {{setter}} { } }
                    {{modifier}} object P34 { get => null; {{setter}} { field = value; } }
                    {{modifier}} object P41 { get { return field; } }
                    {{modifier}} object P42 { get { return field; } {{setter}}; }
                    {{modifier}} object P43 { get { return field; } {{setter}} { } }
                    {{modifier}} object P44 { get { return field; } {{setter}} { field = value; } }
                    {{modifier}} object P51 { get { return null; } }
                    {{modifier}} object P52 { get { return null; } {{setter}}; }
                    {{modifier}} object P53 { get { return null; } {{setter}} { } }
                    {{modifier}} object P54 { get { return null; } {{setter}} { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,25): error CS8051: Auto-implemented properties must have get accessors.
                //            object P02 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(3, 25));
        }

        [Theory]
        [CombinatorialData]
        public void Override_VirtualBase_01(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                class A
                {
                    public virtual object P1 { get; {{setter}}; }
                    public virtual object P2 { get; }
                    public virtual object P3 { {{setter}}; }
                }
                """;
            string sourceB0 = $$"""
                class B0 : A
                {
                    public override object P1 { get; }
                    public override object P2 { get; }
                    public override object P3 { get; }
                }
                """;
            var targetFramework = GetTargetFramework(useInit);
            var comp = CreateCompilation([sourceA, sourceB0], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (5,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P3 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P3").WithLocation(5, 28),
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32),
                // (5,33): error CS0545: 'B0.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B0.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB1 = $$"""
                class B1 : A
                {
                    public override object P1 { get; {{setter}}; }
                    public override object P2 { get; {{setter}}; }
                    public override object P3 { get; {{setter}}; }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB1], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (4,38): error CS0546: 'B1.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { get; set; }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B1.P2.{setter}", "A.P2").WithLocation(4, 38),
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32),
                // (5,33): error CS0545: 'B1.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get; set; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B1.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB2 = $$"""
                class B2 : A
                {
                    public override object P1 { get => field; {{setter}} { } }
                    public override object P2 { get => field; {{setter}} { } }
                    public override object P3 { get => field; {{setter}} { } }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB2], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (4,47): error CS0546: 'B2.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { get => field; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B2.P2.{setter}", "A.P2").WithLocation(4, 47),
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32),
                // (5,33): error CS0545: 'B2.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get => field; set { } }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B2.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB3 = $$"""
                class B3 : A
                {
                    public override object P1 { get => field; }
                    public override object P2 { get => field; }
                    public override object P3 { get => field; }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB3], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { get => field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (5,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P3 { get => field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P3").WithLocation(5, 28),
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32),
                // (5,33): error CS0545: 'B3.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get => field; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B3.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB4 = $$"""
                class B4 : A
                {
                    public override object P1 { {{setter}} { field = value; } }
                    public override object P2 { {{setter}} { field = value; } }
                    public override object P3 { {{setter}} { field = value; } }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB4], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (4,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P2 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P2").WithLocation(4, 28),
                // (4,33): error CS0546: 'B4.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B4.P2.{setter}", "A.P2").WithLocation(4, 33),
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32));
        }

        [Theory]
        [CombinatorialData]
        public void Override_AbstractBase_01(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                abstract class A
                {
                    public abstract object P1 { get; {{setter}}; }
                    public abstract object P2 { get; }
                    public abstract object P3 { {{setter}}; }
                }
                """;
            string sourceB0 = $$"""
                class B0 : A
                {
                    public override object P1 { get; }
                    public override object P2 { get; }
                    public override object P3 { get; }
                }
                """;
            var targetFramework = GetTargetFramework(useInit);
            var comp = CreateCompilation([sourceA, sourceB0], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS0534: 'B0' does not implement inherited abstract member 'A.P3.set'
                // class B0 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B0").WithArguments("B0", $"A.P3.{setter}").WithLocation(1, 7),
                // (1,7): error CS0534: 'B0' does not implement inherited abstract member 'A.P1.set'
                // class B0 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B0").WithArguments("B0", $"A.P1.{setter}").WithLocation(1, 7),
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (5,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P3 { get; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P3").WithLocation(5, 28),
                // (5,33): error CS0545: 'B0.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B0.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB1 = $$"""
                class B1 : A
                {
                    public override object P1 { get; {{setter}}; }
                    public override object P2 { get; {{setter}}; }
                    public override object P3 { get; {{setter}}; }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB1], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (4,38): error CS0546: 'B1.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { get; set; }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B1.P2.{setter}", "A.P2").WithLocation(4, 38),
                // (5,33): error CS0545: 'B1.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get; set; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B1.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB2 = $$"""
                class B2 : A
                {
                    public override object P1 { get => field; {{setter}} { } }
                    public override object P2 { get => field; {{setter}} { } }
                    public override object P3 { get => field; {{setter}} { } }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB2], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (4,47): error CS0546: 'B2.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { get => field; set { } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B2.P2.{setter}", "A.P2").WithLocation(4, 47),
                // (5,33): error CS0545: 'B2.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get => field; set { } }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B2.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB3 = $$"""
                class B3 : A
                {
                    public override object P1 { get => field; }
                    public override object P2 { get => field; }
                    public override object P3 { get => field; }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB3], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS0534: 'B3' does not implement inherited abstract member 'A.P3.set'
                // class B3 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B3").WithArguments("B3", $"A.P3.{setter}").WithLocation(1, 7),
                // (1,7): error CS0534: 'B3' does not implement inherited abstract member 'A.P1.set'
                // class B3 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B3").WithArguments("B3", $"A.P1.{setter}").WithLocation(1, 7),
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { get => field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (5,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P3 { get => field; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P3").WithLocation(5, 28),
                // (5,33): error CS0545: 'B3.P3.get': cannot override because 'A.P3' does not have an overridable get accessor
                //     public override object P3 { get => field; }
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("B3.P3.get", "A.P3").WithLocation(5, 33));

            string sourceB4 = $$"""
                class B4 : A
                {
                    public override object P1 { {{setter}} { field = value; } }
                    public override object P2 { {{setter}} { field = value; } }
                    public override object P3 { {{setter}} { field = value; } }
                }
                """;
            comp = CreateCompilation([sourceA, sourceB4], targetFramework: targetFramework);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS0534: 'B4' does not implement inherited abstract member 'A.P1.get'
                // class B4 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B4").WithArguments("B4", "A.P1.get").WithLocation(1, 7),
                // (1,7): error CS0534: 'B4' does not implement inherited abstract member 'A.P2.get'
                // class B4 : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B4").WithArguments("B4", "A.P2.get").WithLocation(1, 7),
                // (3,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P1 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithLocation(3, 28),
                // (4,28): error CS8080: Auto-implemented properties must override all accessors of the overridden property.
                //     public override object P2 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P2").WithLocation(4, 28),
                // (4,33): error CS0546: 'B4.P2.set': cannot override because 'A.P2' does not have an overridable set accessor
                //     public override object P2 { set { field = value; } }
                Diagnostic(ErrorCode.ERR_NoSetToOverride, setter).WithArguments($"B4.P2.{setter}", "A.P2").WithLocation(4, 33));
        }

        [Theory]
        [CombinatorialData]
        public void New_01(bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string source = $$"""
                class A
                {
                    public virtual object P1 { get; {{setter}}; }
                    public virtual object P2 { get; }
                    public virtual object P3 { {{setter}}; }
                }
                class B0 : A
                {
                    public new object P1 { get; }
                    public new object P2 { get; }
                    public new object P3 { get; }
                }
                class B1 : A
                {
                    public new object P1 { get; {{setter}}; }
                    public new object P2 { get; {{setter}}; }
                    public new object P3 { get; {{setter}}; }
                }
                class B2 : A
                {
                    public new object P1 { get => field; {{setter}} { } }
                    public new object P2 { get => field; {{setter}} { } }
                    public new object P3 { get => field; {{setter}} { } }
                }
                class B3 : A
                {
                    public new object P1 { get => field; }
                    public new object P2 { get => field; }
                    public new object P3 { get => field; }
                }
                class B4 : A
                {
                    public new object P1 { {{setter}} { field = value; } }
                    public new object P2 { {{setter}} { field = value; } }
                    public new object P3 { {{setter}} { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (5,32): error CS8051: Auto-implemented properties must have get accessors.
                //     public virtual object P3 { set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, setter).WithLocation(5, 32));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void CompilerGeneratedAttribute(bool missingType, bool missingConstructor)
        {
            string source = """
                using System;
                using System.Reflection;

                class C
                {
                    public int P1 { get; }
                    public int P2 { get => field; }
                    public int P3 { set { field = value; } }
                    public int P4 { init { field = value; } }
                }

                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            ReportField(field);
                    }

                    static void ReportField(FieldInfo field)
                    {
                        Console.Write("{0}.{1}:", field.DeclaringType.Name, field.Name);
                        foreach (var obj in field.GetCustomAttributes())
                            Console.Write(" {0},", obj.ToString());
                        Console.WriteLine();
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            if (missingType)
            {
                comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute);
            }
            if (missingConstructor)
            {
                comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor);
            }
            string expectedAttributes = (missingType || missingConstructor) ? "" : " System.Runtime.CompilerServices.CompilerGeneratedAttribute,";
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput($$"""
                C.<P1>k__BackingField:{{expectedAttributes}}
                C.<P2>k__BackingField:{{expectedAttributes}}
                C.<P3>k__BackingField:{{expectedAttributes}}
                C.<P4>k__BackingField:{{expectedAttributes}}
                """));
        }

        [Theory]
        [CombinatorialData]
        public void Conditional(bool useDEBUG)
        {
            string sourceA = """
                using System.Diagnostics;
                class C
                {
                    public static object P1 { get { M(field); return null; } set { } }
                    public static object P2 { get { return null; } set { M(field); } }
                    public object P3 { get { M(field); return null; } }
                    public object P4 { set { M(field); } }
                    public object P5 { init { M(field); } }
                    [Conditional("DEBUG")]
                    static void M( object o) { }
                }
                """;
            string sourceB = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            ReportField(field);
                    }
                    static void ReportField(FieldInfo field)
                    {
                        Console.Write("{0}.{1}:", field.DeclaringType.Name, field.Name);
                        foreach (var obj in field.GetCustomAttributes())
                            Console.Write(" {0},", obj.ToString());
                        Console.WriteLine();
                    }
                }
                """;
            var parseOptions = TestOptions.RegularNext;
            if (useDEBUG)
            {
                parseOptions = parseOptions.WithPreprocessorSymbols("DEBUG");
            }
            var verifier = CompileAndVerify(
                [sourceA, sourceB],
                parseOptions: parseOptions,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput("""
                    C.<P3>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                    C.<P4>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                    C.<P5>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                    C.<P1>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                    C.<P2>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute,
                    """));
            if (useDEBUG)
            {
                verifier.VerifyIL("C.P1.get", """
                    {
                      // Code size       12 (0xc)
                      .maxstack  1
                      IL_0000:  ldsfld     "object C.<P1>k__BackingField"
                      IL_0005:  call       "void C.M(object)"
                      IL_000a:  ldnull
                      IL_000b:  ret
                    }
                    """);
                verifier.VerifyIL("C.P4.set", """
                    {
                      // Code size       12 (0xc)
                      .maxstack  1
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "object C.<P4>k__BackingField"
                      IL_0006:  call       "void C.M(object)"
                      IL_000b:  ret
                    }
                    """);
            }
            else
            {
                verifier.VerifyIL("C.P1.get", """
                    {
                      // Code size        2 (0x2)
                      .maxstack  1
                      IL_0000:  ldnull
                      IL_0001:  ret
                    }
                    """);
                verifier.VerifyIL("C.P4.set", """
                    {
                      // Code size        1 (0x1)
                      .maxstack  0
                      IL_0000:  ret
                    }
                    """);
            }
            var comp = (CSharpCompilation)verifier.Compilation;
            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<FieldSymbol>().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P5>k__BackingField",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void RestrictedTypes()
        {
            string source = """
                using System;

                class C
                {
                    static TypedReference P1 { get; }
                    ArgIterator P2 { get; set; }
                    static TypedReference Q1 => field;
                    ArgIterator Q2 { get { return field; } set { } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (5,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     static TypedReference P1 { get; }
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "TypedReference").WithArguments("System.TypedReference").WithLocation(5, 12),
                // (6,5): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     ArgIterator P2 { get; set; }
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(6, 5),
                // (7,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     static TypedReference Q1 => field;
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "TypedReference").WithArguments("System.TypedReference").WithLocation(7, 12),
                // (8,5): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     ArgIterator Q2 { get { return field; } set { } }
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(8, 5));
        }

        [Theory]
        [InlineData("class", false)]
        [InlineData("struct", false)]
        [InlineData("ref struct", true)]
        [InlineData("record", false)]
        [InlineData("record struct", false)]
        public void ByRefLikeType_01(string typeKind, bool allow)
        {
            string source = $$"""
                ref struct R
                {
                }

                {{typeKind}} C
                {
                    R P1 { get; }
                    R P2 { get; set; }
                    R Q1 => field;
                    R Q2 { get => field; }
                    R Q3 { set { _ = field; } }
                    public override string ToString() => "C";
                }

                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        System.Console.WriteLine("{0}", c.ToString());
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            if (allow)
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: "C");
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (7,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                    //     R P1 { get; }
                    Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(7, 5),
                    // (8,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                    //     R P2 { get; set; }
                    Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(8, 5),
                    // (9,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                    //     R Q1 => field;
                    Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(9, 5),
                    // (10,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                    //     R Q2 { get => field; }
                    Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(10, 5),
                    // (11,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                    //     R Q3 { set { _ = field; } }
                    Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(11, 5));
            }
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("ref struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void ByRefLikeType_02(string typeKind)
        {
            string source = $$"""
                ref struct R
                {
                }

                {{typeKind}} C
                {
                    static R P1 { get; }
                    static R P2 { get; set; }
                    static R Q1 => field;
                    static R Q2 { get => field; }
                    static R Q3 { set { _ = field; } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R P1 { get; }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(7, 12),
                // (8,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R P2 { get; set; }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(8, 12),
                // (9,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R Q1 => field;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(9, 12),
                // (10,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R Q2 { get => field; }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(10, 12),
                // (11,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R Q3 { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(11, 12));
        }

        [Fact]
        public void ByRefLikeType_03()
        {
            string source = """
                ref struct R
                {
                }

                interface I
                {
                    static R P1 { get; }
                    R P2 { get; set; }
                    static R Q1 => field;
                    R Q2 { get => field; }
                    R Q3 { set { _ = field; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (7,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R P1 { get; }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(7, 12),
                // (9,12): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     static R Q1 => field;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(9, 12),
                // (10,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     R Q2 { get => field; }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(10, 5),
                // (11,5): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                //     R Q3 { set { _ = field; } }
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(11, 5));
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersionFacts.CSharpNext)] LanguageVersion languageVersion,
            bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    public partial object P3 { get; {{setter}}; }
                    public partial object P3 { get; {{setter}} { } }
                    public partial object P4 { get; {{setter}}; }
                    public partial object P4 { get => null; {{setter}}; }
                }
                """;
            string sourceB = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        var c = new C { P3 = 3, P4 = 4 };
                        Console.WriteLine((c.P3, c.P4));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}", field.Name);
                    }
                }
                """;
            var comp = CreateCompilation(
                [sourceA, sourceB],
                parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                options: TestOptions.ReleaseExe,
                targetFramework: GetTargetFramework(useInit));

            if (languageVersion == LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public partial object P3 { get; set { } }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P3").WithArguments("field keyword").WithLocation(4, 27),
                    // (6,27): error CS8652: The feature 'field keyword' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public partial object P4 { get => null; set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "P4").WithArguments("field keyword").WithLocation(6, 27));
            }
            else
            {
                CompileAndVerify(
                    comp,
                    verify: Verification.Skipped,
                    expectedOutput: IncludeExpectedOutput(useInit, """
                    (, )
                    <P3>k__BackingField
                    <P4>k__BackingField
                    """));
            }

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(2, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_02(bool reverseOrder, bool useInit)
        {
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    public partial object P1 { get; }
                    public partial object P2 { {{setter}}; }
                    public partial object P3 { get; {{setter}}; }
                    public partial object P4 { get; {{setter}}; }
                    public partial object P5 { get; {{setter}}; }
                }
                """;
            string sourceB = $$"""
                partial class C
                {
                    public partial object P1 { get => field; }
                    public partial object P2 { {{setter}} { field = value; } }
                    public partial object P3 { get; {{setter}} { field = value; } }
                    public partial object P4 { get => field; {{setter}}; }
                    public partial object P5 { get => field; {{setter}} { field = value; } }
                }
                """;
            string sourceC = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        var c = new C { P2 = 2, P3 = 3, P4 = 4, P5 = 5 };
                        Console.WriteLine((c.P3, c.P4, c.P5));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}", field.Name);
                    }
                }
                """;
            var verifier = CompileAndVerify(
                reverseOrder ? [sourceC, sourceB, sourceA] : [sourceA, sourceB, sourceC],
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, """
                    (3, 4, 5)
                    <P1>k__BackingField
                    <P2>k__BackingField
                    <P3>k__BackingField
                    <P4>k__BackingField
                    <P5>k__BackingField
                    """));
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P5>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(5, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "P5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        private static void VerifyMergedProperties(ImmutableArray<PropertySymbol> properties, ImmutableArray<FieldSymbol> fields)
        {
            int fieldIndex = 0;
            for (int propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++)
            {
                var property = (SourcePropertySymbol)properties[propertyIndex];
                var field = (property.BackingField is null) ? null : (SynthesizedBackingFieldSymbol)fields[fieldIndex++];
                Assert.Equal(property.IsPartial, property.IsPartialDefinition);
                VerifyMergedProperty(property, field);
            }
            Assert.Equal(fields.Length, fieldIndex);
        }

        private static void VerifyMergedProperty(SourcePropertySymbol property, SynthesizedBackingFieldSymbol fieldOpt)
        {
            Assert.Same(property.BackingField, fieldOpt);
            if (property.OtherPartOfPartial is { } otherPart)
            {
                Assert.True(otherPart.IsPartial);
                Assert.Equal(property.IsPartialDefinition, !otherPart.IsPartialDefinition);
                Assert.Equal(property.IsPartialImplementation, !otherPart.IsPartialImplementation);
                Assert.Same(property.BackingField, otherPart.BackingField);
            }
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_ConstructorAssignment(
            [CombinatorialValues("partial class", "partial struct", "ref partial struct", "partial record", "partial record struct")] string typeKind,
            bool reverseOrder,
            bool useStatic)
        {
            string modifier = useStatic ? "static" : "      ";
            string constructorModifier = useStatic ? "static" : "public";
            string sourceA = $$"""
                {{typeKind}} C
                {
                    internal {{modifier}} partial object P1 { get; }
                    internal {{modifier}} partial object P2 { get => field; }
                }
                """;
            string sourceB = $$"""
                {{typeKind}} C
                {
                    internal {{modifier}} partial object P1 { get => field; }
                    internal {{modifier}} partial object P2 { get; }
                    {{constructorModifier}} C()
                    {
                        P1 = 1;
                        P2 = 2;
                    }
                }
                """;
            string sourceC = useStatic ?
                """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2));
                    }
                }
                """ :
                """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2));
                    }
                }
                """;
            var verifier = CompileAndVerify(
                reverseOrder ? [sourceC, sourceB, sourceA] : [sourceA, sourceB, sourceC],
                expectedOutput: "(1, 2)");
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().OrderBy(f => f.Name).ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().Where(p => p.Name != "EqualityContract").OrderBy(p => p.Name).ToImmutableArray();
            Assert.Equal(2, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void PartialProperty_Initializer_01(bool useStatic, bool useInit)
        {
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string source = $$"""
                partial class C
                {
                    {{modifier}} partial object P1 { get; } = 1;
                    {{modifier}} partial object P1 { get; }
                    {{modifier}} partial object P2 { {{setter}}; } = 2;
                    {{modifier}} partial object P2 { {{setter}}; }
                    {{modifier}} partial object P3 { get; {{setter}}; } = 3;
                    {{modifier}} partial object P3 { get; {{setter}}; }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS9248: Partial property 'C.P1' must have an implementation part.
                //            partial object P1 { get; } = 1;
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P1").WithArguments("C.P1").WithLocation(3, 27),
                // (3,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P1 { get; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P1").WithLocation(3, 27),
                // (4,27): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //            partial object P1 { get; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P1").WithLocation(4, 27),
                // (4,27): error CS0102: The type 'C' already contains a definition for 'P1'
                //            partial object P1 { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(4, 27),
                // (5,27): error CS9248: Partial property 'C.P2' must have an implementation part.
                //            partial object P2 { set; } = 2;
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P2").WithArguments("C.P2").WithLocation(5, 27),
                // (5,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P2 { set; } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P2").WithLocation(5, 27),
                // (6,27): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //            partial object P2 { set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P2").WithLocation(6, 27),
                // (6,27): error CS0102: The type 'C' already contains a definition for 'P2'
                //            partial object P2 { set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("C", "P2").WithLocation(6, 27),
                // (7,27): error CS9248: Partial property 'C.P3' must have an implementation part.
                //            partial object P3 { get; set; } = 3;
                Diagnostic(ErrorCode.ERR_PartialPropertyMissingImplementation, "P3").WithArguments("C.P3").WithLocation(7, 27),
                // (7,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P3 { get; set; } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P3").WithLocation(7, 27),
                // (8,27): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //            partial object P3 { get; set; }
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P3").WithLocation(8, 27),
                // (8,27): error CS0102: The type 'C' already contains a definition for 'P3'
                //            partial object P3 { get; set; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(8, 27));

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(6, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[5] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Initializer_02(bool reverseOrder, bool useStatic, bool useInit)
        {
            if (useStatic && useInit) return;
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    {{modifier}} partial object P1 { get; } = 1;
                    {{modifier}} partial object P2 { {{setter}}; } = 2;
                    {{modifier}} partial object P3 { get; {{setter}}; } = 3;
                    {{modifier}} partial object Q1 { get => null; } = 1;
                    {{modifier}} partial object Q2 { {{setter}} { } } = 2;
                    {{modifier}} partial object Q3 { get => null; {{setter}} { } } = 3;
                }
                """;
            string sourceB = $$"""
                partial class C
                {
                    {{modifier}} partial object P1 { get => null; }
                    {{modifier}} partial object P2 { {{setter}} { } }
                    {{modifier}} partial object P3 { get => null; {{setter}} { } }
                    {{modifier}} partial object Q1 { get; }
                    {{modifier}} partial object Q2 { {{setter}}; }
                    {{modifier}} partial object Q3 { get; {{setter}}; }
                }
                """;
            var comp = CreateCompilation(
                reverseOrder ? [sourceB, sourceA] : [sourceA, sourceB],
                targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P1 { get; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P1").WithLocation(3, 27),
                // (4,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P2 { set; } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P2").WithLocation(4, 27),
                // (5,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P3 { get; set; } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P3").WithLocation(5, 27),
                // (6,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object Q1 { get => null; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "Q1").WithLocation(6, 27),
                // (7,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object Q2 { set { } } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "Q2").WithLocation(7, 27),
                // (8,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object Q3 { get => null; set { } } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "Q3").WithLocation(8, 27));

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<Q1>k__BackingField",
                "System.Object C.<Q2>k__BackingField",
                "System.Object C.<Q3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().OrderBy(p => p.Name).ToImmutableArray();
            Assert.Equal(6, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "Q1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "Q2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[5] is SourcePropertySymbol { Name: "Q3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Initializer_03(bool reverseOrder, bool useStatic, bool useInit)
        {
            if (useStatic && useInit) return;
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    public {{modifier}} partial object P1 { get; } = 1;
                    public {{modifier}} partial object P2 { {{setter}}; } = 2;
                    public {{modifier}} partial object P3 { get; {{setter}}; } = 3;
                    public {{modifier}} partial object P4 { get; {{setter}}; } = 4;
                    public {{modifier}} partial object P5 { get; {{setter}}; } = 5;
                    public {{modifier}} partial object P6 { get; {{setter}}; } = 6;
                    public {{modifier}} partial object P7 { get; {{setter}}; } = 7;
                    public {{modifier}} partial object Q1 { get => field; } = 1;
                    public {{modifier}} partial object Q2 { {{setter}} { field = value; } } = 2;
                    public {{modifier}} partial object Q3 { get; {{setter}} { field = value; } } = 3;
                    public {{modifier}} partial object Q4 { get => field; {{setter}}; } = 4;
                    public {{modifier}} partial object Q5 { get => field; {{setter}} { field = value; } } = 5;
                    public {{modifier}} partial object Q6 { get; {{setter}} { } } = 6;
                    public {{modifier}} partial object Q7 { get => null; {{setter}}; } = 7;
                }
                """;
            string sourceB = $$"""
                partial class C
                {
                    public {{modifier}} partial object P1 { get => field; }
                    public {{modifier}} partial object P2 { {{setter}} { field = value; } }
                    public {{modifier}} partial object P3 { get; {{setter}} { field = value; } }
                    public {{modifier}} partial object P4 { get => field; {{setter}}; }
                    public {{modifier}} partial object P5 { get => field; {{setter}} { field = value; } }
                    public {{modifier}} partial object P6 { get; {{setter}} { } }
                    public {{modifier}} partial object P7 { get => null; {{setter}}; }
                    public {{modifier}} partial object Q1 { get; }
                    public {{modifier}} partial object Q2 { {{setter}}; }
                    public {{modifier}} partial object Q3 { get; {{setter}}; }
                    public {{modifier}} partial object Q4 { get; {{setter}}; }
                    public {{modifier}} partial object Q5 { get; {{setter}}; }
                    public {{modifier}} partial object Q6 { get; {{setter}}; }
                    public {{modifier}} partial object Q7 { get; {{setter}}; }
                }
                """;
            string receiver = useStatic ? "C" : "c";
            string sourceC = $$"""
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine(({{receiver}}.P1, {{receiver}}.P3, {{receiver}}.P4, {{receiver}}.P5, {{receiver}}.P6, {{receiver}}.P7, {{receiver}}.Q1, {{receiver}}.Q3, {{receiver}}.Q4, {{receiver}}.Q5, {{receiver}}.Q6, {{receiver}}.Q7));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            Console.WriteLine("{0}", field.Name);
                    }
                }
                """;
            var verifier = CompileAndVerify(
                reverseOrder ? [sourceC, sourceB, sourceA] : [sourceA, sourceB, sourceC],
                targetFramework: GetTargetFramework(useInit),
                verify: Verification.Skipped,
                expectedOutput: IncludeExpectedOutput(useInit, """
                    (1, 3, 4, 5, 6, , 1, 3, 4, 5, 6, )
                    <P1>k__BackingField
                    <P2>k__BackingField
                    <P3>k__BackingField
                    <P4>k__BackingField
                    <P5>k__BackingField
                    <P6>k__BackingField
                    <P7>k__BackingField
                    <Q1>k__BackingField
                    <Q2>k__BackingField
                    <Q3>k__BackingField
                    <Q4>k__BackingField
                    <Q5>k__BackingField
                    <Q6>k__BackingField
                    <Q7>k__BackingField
                    """));
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P5>k__BackingField",
                "System.Object C.<P6>k__BackingField",
                "System.Object C.<P7>k__BackingField",
                "System.Object C.<Q1>k__BackingField",
                "System.Object C.<Q2>k__BackingField",
                "System.Object C.<Q3>k__BackingField",
                "System.Object C.<Q4>k__BackingField",
                "System.Object C.<Q5>k__BackingField",
                "System.Object C.<Q6>k__BackingField",
                "System.Object C.<Q7>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().OrderBy(p => p.Name).ToImmutableArray();
            Assert.Equal(14, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "P5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[5] is SourcePropertySymbol { Name: "P6", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[6] is SourcePropertySymbol { Name: "P7", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[7] is SourcePropertySymbol { Name: "Q1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[8] is SourcePropertySymbol { Name: "Q2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[9] is SourcePropertySymbol { Name: "Q3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[10] is SourcePropertySymbol { Name: "Q4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[11] is SourcePropertySymbol { Name: "Q5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[12] is SourcePropertySymbol { Name: "Q6", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[13] is SourcePropertySymbol { Name: "Q7", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Initializer_04(bool reverseOrder, bool useStatic, bool useInit)
        {
            if (useStatic && useInit) return;
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    {{modifier}} partial object P1 { get; } = 1;
                    {{modifier}} partial object P2 { {{setter}}; } = 2;
                    {{modifier}} partial object P3 { get; {{setter}}; } = 3;
                }
                """;
            string sourceB = $$"""
                partial class C
                {
                    {{modifier}} partial object P1 { get => null; } = 1;
                    {{modifier}} partial object P2 { {{setter}} { } } = 2;
                    {{modifier}} partial object P3 { get => null; {{setter}} { } } = 3;
                }
                """;
            var comp = CreateCompilation(
                reverseOrder ? [sourceB, sourceA] : [sourceA, sourceB],
                targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //            partial object P1 { get => null; } = 1;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P1").WithLocation(3, 27),
                // (3,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P1 { get; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P1").WithLocation(3, 27),
                // (3,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P1 { get => null; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P1").WithLocation(3, 27),
                // (4,27): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //            partial object P2 { set { } } = 2;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P2").WithLocation(4, 27),
                // (4,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P2 { set; } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P2").WithLocation(4, 27),
                // (4,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P2 { set { } } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P2").WithLocation(4, 27),
                // (5,27): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //            partial object P3 { get => null; set { } } = 3;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P3").WithLocation(5, 27),
                // (5,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P3 { get; set; } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P3").WithLocation(5, 27),
                // (5,27): error CS8050: Only auto-implemented properties, or properties that use the 'field' keyword, can have initializers.
                //            partial object P3 { get => null; set { } } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P3").WithLocation(5, 27));

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(3, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Initializer_05(bool reverseOrder, bool useStatic, bool useInit)
        {
            if (useStatic && useInit) return;
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                partial class C
                {
                    public {{modifier}} partial object P1 { get; } = 1;
                    public {{modifier}} partial object P2 { {{setter}}; } = 2;
                    public {{modifier}} partial object P3 { get; {{setter}}; } = 3;
                    public {{modifier}} partial object P4 { get; {{setter}}; } = 4;
                    public {{modifier}} partial object P5 { get; {{setter}}; } = 5;
                }
                """;
            string sourceB = $$"""
                partial class C
                {
                    public {{modifier}} partial object P1 { get => field; } = -1;
                    public {{modifier}} partial object P2 { {{setter}} { field = value; } } = -2;
                    public {{modifier}} partial object P3 { get; {{setter}} { field = value; } } = -3;
                    public {{modifier}} partial object P4 { get => field; {{setter}}; } = -4;
                    public {{modifier}} partial object P5 { get => field; {{setter}} { field = value; } } = -5;
                }
                """;

            var comp = CreateCompilation(
                reverseOrder ? [sourceB, sourceA] : [sourceA, sourceB],
                targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,34): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //     public        partial object P1 { get => field; } = -1;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P1").WithLocation(3, 34),
                // (4,34): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //     public        partial object P2 { set { field = value; } } = -2;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P2").WithLocation(4, 34),
                // (5,34): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //     public        partial object P3 { get; set { field = value; } } = -3;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P3").WithLocation(5, 34),
                // (6,34): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //     public        partial object P4 { get => field; set; } = -4;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P4").WithLocation(6, 34),
                // (7,34): error CS9263: A partial property cannot have an initializer on both the definition and implementation.
                //     public        partial object P5 { get => field; set { field = value; } } = -5;
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateInitializer, "P5").WithLocation(7, 34));

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P5>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(5, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "P5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);

            var actualValues = getInitializerValues(comp, comp.SyntaxTrees[reverseOrder ? 1 : 0]);
            var expectedValues = new[]
            {
                   ((object)1, "System.Int32", "System.Object"),
                   ((object)2, "System.Int32", "System.Object"),
                   ((object)3, "System.Int32", "System.Object"),
                   ((object)4, "System.Int32", "System.Object"),
                   ((object)5, "System.Int32", "System.Object"),
               };
            AssertEx.Equal(expectedValues, actualValues);

            actualValues = getInitializerValues(comp, comp.SyntaxTrees[reverseOrder ? 0 : 1]);
            expectedValues = new[]
            {
                   ((object)-1, "System.Int32", "System.Object"),
                   ((object)-2, "System.Int32", "System.Object"),
                   ((object)-3, "System.Int32", "System.Object"),
                   ((object)-4, "System.Int32", "System.Object"),
                   ((object)-5, "System.Int32", "System.Object"),
               };
            AssertEx.Equal(expectedValues, actualValues);

            static (object, string, string)[] getInitializerValues(CSharpCompilation comp, SyntaxTree tree)
            {
                var model = comp.GetSemanticModel(tree);
                return tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().
                    Select(p =>
                    {
                        var value = p.Initializer.Value;
                        var typeInfo = model.GetTypeInfo(value);
                        return (model.GetConstantValue(value).Value, typeInfo.Type.ToTestDisplayString(), typeInfo.ConvertedType.ToTestDisplayString());

                    }).ToArray();
            }
        }

        [Fact]
        public void PartialProperty_Initializer_06()
        {
            string source = $$"""
                partial class C
                {
                    partial object P1 { get; set; } = 1; // A1
                    partial object P2 { get; set; }      // A2
                    partial object P3 { get; set; } = 3; // A3
                    partial object P4 { get; set; }      // A4
                    partial object P5 { get { return field; } set { field = value; } } = 5; // A5
                    partial object P6 { get { return field; } set { field = value; } }      // A6

                    partial object P1 { get; set; }      // B1
                    partial object P2 { get; set; }      // B2
                    partial object P3 { get { return field; } set { field = value; } }      // B3
                    partial object P4 { get { return field; } set { field = value; } }      // B4
                    partial object P5 { get; set; }      // B5
                    partial object P6 { get { return field; } set { field = value; } }      // B6

                    partial object P1 { get { return field; } set { field = value; } }      // C1
                    partial object P2 { get { return field; } set { field = value; } } = 2; // C2
                    partial object P3 { get { return field; } set { field = value; } }      // C3
                    partial object P4 { get { return field; } set { field = value; } } = 4; // C4
                    partial object P5 { get; set; }      // C5
                    partial object P6 { get; set; } = 6; // C6
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,20): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial object P1 { get; set; }      // B1
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P1").WithLocation(10, 20),
                // (10,20): error CS0102: The type 'C' already contains a definition for 'P1'
                //     partial object P1 { get; set; }      // B1
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(10, 20),
                // (11,20): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial object P2 { get; set; }      // B2
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P2").WithLocation(11, 20),
                // (11,20): error CS0102: The type 'C' already contains a definition for 'P2'
                //     partial object P2 { get; set; }      // B2
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("C", "P2").WithLocation(11, 20),
                // (15,20): error CS9251: A partial property may not have multiple implementing declarations
                //     partial object P6 { get { return field; } set { field = value; } }      // B6
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P6").WithLocation(15, 20),
                // (19,20): error CS9251: A partial property may not have multiple implementing declarations
                //     partial object P3 { get { return field; } set { field = value; } }      // C3
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P3").WithLocation(19, 20),
                // (19,20): error CS0102: The type 'C' already contains a definition for 'P3'
                //     partial object P3 { get { return field; } set { field = value; } }      // C3
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(19, 20),
                // (20,20): error CS9251: A partial property may not have multiple implementing declarations
                //     partial object P4 { get { return field; } set { field = value; } } = 4; // C4
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateImplementation, "P4").WithLocation(20, 20),
                // (20,20): error CS0102: The type 'C' already contains a definition for 'P4'
                //     partial object P4 { get { return field; } set { field = value; } } = 4; // C4
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P4").WithArguments("C", "P4").WithLocation(20, 20),
                // (21,20): error CS9250: A partial property may not have multiple defining declarations, and cannot be an auto-property.
                //     partial object P5 { get; set; }      // C5
                Diagnostic(ErrorCode.ERR_PartialPropertyDuplicateDefinition, "P5").WithLocation(21, 20),
                // (21,20): error CS0102: The type 'C' already contains a definition for 'P5'
                //     partial object P5 { get; set; }      // C5
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P5").WithArguments("C", "P5").WithLocation(21, 20),
                // (22,20): error CS0102: The type 'C' already contains a definition for 'P6'
                //     partial object P6 { get; set; } = 6; // C6
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P6").WithArguments("C", "P6").WithLocation(22, 20));

            var containingType = comp.GetMember<NamedTypeSymbol>("C");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object C.<P1>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P5>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P6>k__BackingField",
                "System.Object C.<P2>k__BackingField",
                "System.Object C.<P3>k__BackingField",
                "System.Object C.<P4>k__BackingField",
                "System.Object C.<P6>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(12, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[5] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[6] is SourcePropertySymbol { Name: "P5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[7] is SourcePropertySymbol { Name: "P6", IsPartialDefinition: false, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[8] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: false, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[9] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: false, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[10] is SourcePropertySymbol { Name: "P5", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[11] is SourcePropertySymbol { Name: "P6", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperty((SourcePropertySymbol)actualProperties[0], (SynthesizedBackingFieldSymbol)actualFields[0]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[1], (SynthesizedBackingFieldSymbol)actualFields[5]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[2], (SynthesizedBackingFieldSymbol)actualFields[1]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[3], (SynthesizedBackingFieldSymbol)actualFields[3]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[4], null);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[5], null);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[6], (SynthesizedBackingFieldSymbol)actualFields[2]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[7], (SynthesizedBackingFieldSymbol)actualFields[4]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[8], (SynthesizedBackingFieldSymbol)actualFields[6]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[9], (SynthesizedBackingFieldSymbol)actualFields[7]);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[10], null);
            VerifyMergedProperty((SourcePropertySymbol)actualProperties[11], (SynthesizedBackingFieldSymbol)actualFields[8]);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_ReadOnly(bool reverseOrder, bool useReadOnlyDefinition, bool useReadOnlyImplementation)
        {
            string modifierDefinition = useReadOnlyDefinition ? "readonly" : "        ";
            string modifierImplementation = useReadOnlyImplementation ? "readonly" : "        ";
            string sourceA = $$"""
                partial struct S
                {
                    {{modifierDefinition}} partial object P1 { get; }
                    {{modifierDefinition}} partial object P2 { set; }
                    partial object P3 { {{modifierDefinition}} get; }
                    partial object P4 { {{modifierDefinition}} set; }
                    {{modifierDefinition}} partial object P5 { get; set; }
                    partial object P6 { {{modifierDefinition}} get; set; }
                    partial object P7 { get; {{modifierDefinition}} set; }
                    partial object P8 { {{modifierDefinition}} get; set; }
                    partial object P9 { get; {{modifierDefinition}} set; }
                }
                """;
            string sourceB = $$"""
                partial struct S
                {
                    {{modifierImplementation}} partial object P1 { get => field; }
                    {{modifierImplementation}} partial object P2 { set { _ = field; } }
                    partial object P3 { {{modifierImplementation}} get => field; }
                    partial object P4 { {{modifierImplementation}} set { _ = field; } }
                    {{modifierImplementation}} partial object P5 { get; set { } }
                    partial object P6 { {{modifierImplementation}} get; set { } }
                    partial object P7 { get; {{modifierImplementation}} set { } }
                    partial object P8 { {{modifierImplementation}} get => field; set { } }
                    partial object P9 { get => field; {{modifierImplementation}} set { } }
                }
                """;
            var comp = CreateCompilation(reverseOrder ? [sourceB, sourceA] : [sourceA, sourceB]);
            switch (useReadOnlyDefinition, useReadOnlyImplementation)
            {
                case (false, false):
                    comp.VerifyEmitDiagnostics();
                    break;
                case (false, true):
                    comp.VerifyEmitDiagnostics(
                        // (3,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     readonly partial object P1 { get => field; }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P1").WithLocation(3, 29),
                        // (4,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     readonly partial object P2 { set { _ = field; } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P2").WithLocation(4, 29),
                        // (5,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P3 { readonly get => field; }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(5, 34),
                        // (6,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P4 { readonly set { _ = field; } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(6, 34),
                        // (7,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     readonly partial object P5 { get; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P5").WithLocation(7, 29),
                        // (8,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P6 { readonly get; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(8, 34),
                        // (9,39): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P7 { get; readonly set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(9, 39),
                        // (10,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P8 { readonly get => field; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(10, 34),
                        // (11,48): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P9 { get => field; readonly set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(11, 48));
                    break;
                case (true, false):
                    comp.VerifyEmitDiagnostics(
                        // (3,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //              partial object P1 { get => field; }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P1").WithLocation(3, 29),
                        // (4,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //              partial object P2 { set { _ = field; } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P2").WithLocation(4, 29),
                        // (5,20): error CS8664: 'S.P3': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                        //     partial object P3 { readonly get; }
                        Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P3").WithArguments("S.P3").WithLocation(5, 20),
                        // (5,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P3 {          get => field; }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(5, 34),
                        // (6,20): error CS8664: 'S.P4': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                        //     partial object P4 { readonly set; }
                        Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P4").WithArguments("S.P4").WithLocation(6, 20),
                        // (6,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P4 {          set { _ = field; } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(6, 34),
                        // (7,29): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //              partial object P5 { get; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "P5").WithLocation(7, 29),
                        // (8,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P6 {          get; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(8, 34),
                        // (9,39): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P7 { get;          set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(9, 39),
                        // (10,34): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P8 {          get => field; set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "get").WithLocation(10, 34),
                        // (11,48): error CS8663: Both partial member declarations must be readonly or neither may be readonly
                        //     partial object P9 { get => field;          set { } }
                        Diagnostic(ErrorCode.ERR_PartialMemberReadOnlyDifference, "set").WithLocation(11, 48));
                    break;
                case (true, true):
                    comp.VerifyEmitDiagnostics(
                        // (5,20): error CS8664: 'S.P3': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                        //     partial object P3 { readonly get; }
                        Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P3").WithArguments("S.P3").WithLocation(5, 20),
                        // (6,20): error CS8664: 'S.P4': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                        //     partial object P4 { readonly set; }
                        Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P4").WithArguments("S.P4").WithLocation(6, 20));
                    break;
            }

            var containingType = comp.GetMember<NamedTypeSymbol>("S");
            var actualMembers = comp.GetMember<NamedTypeSymbol>("S").
                GetMembers().
                OfType<PropertySymbol>().
                Select(p =>
                {
                    var property = (SourcePropertySymbol)p;
                    var field = property.BackingField;
                    return $"{field.ToTestDisplayString()}: IsAutoProperty: {property.IsAutoProperty}, UsesFieldKeyword: {property.UsesFieldKeyword}, BackingField.IsReadOnly: {field.IsReadOnly}";
                }).
                ToArray();
            var expectedMembers = new[]
            {
                $"System.Object S.<P1>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P2>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P3>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P4>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P5>k__BackingField: IsAutoProperty: True, UsesFieldKeyword: False, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P6>k__BackingField: IsAutoProperty: True, UsesFieldKeyword: False, BackingField.IsReadOnly: False",
                $"System.Object S.<P7>k__BackingField: IsAutoProperty: True, UsesFieldKeyword: False, BackingField.IsReadOnly: {useReadOnlyImplementation}",
                $"System.Object S.<P8>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: False",
                $"System.Object S.<P9>k__BackingField: IsAutoProperty: False, UsesFieldKeyword: True, BackingField.IsReadOnly: False",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Attribute_01(bool reverseOrder, bool useStatic)
        {
            string modifier = useStatic ? "static" : "      ";
            string sourceA = $$"""
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                """;
            string sourceB1 = $$"""
                partial class B
                {
                    {{modifier}} partial object P1 { get; }
                    {{modifier}} partial object P2 { get; set; }
                    {{modifier}} partial object P3 { [A(field)] get; }
                    {{modifier}} partial object P4 { get; [A(field)] set; }
                }
                """;
            string sourceB2 = $$"""
                partial class B
                {
                    {{modifier}} partial object P1 { [A(field)] get { return null; } }
                    {{modifier}} partial object P2 { get { return null; } [A(field)] set { } }
                    {{modifier}} partial object P3 { get { return null; } }
                    {{modifier}} partial object P4 { get { return null; } set { } }
                }
                """;
            var comp = CreateCompilation(reverseOrder ? [sourceB2, sourceB1, sourceA] : [sourceA, sourceB1, sourceB2]);
            comp.VerifyEmitDiagnostics(
                // (3,35): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //            partial object P1 { [A(field)] get { return null; } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(3, 35),
                // (4,56): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //            partial object P2 { get { return null; } [A(field)] set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(4, 56),
                // (5,35): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //            partial object P3 { [A(field)] get; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(5, 35),
                // (6,40): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //            partial object P4 { get; [A(field)] set; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(6, 40));

            var containingType = comp.GetMember<NamedTypeSymbol>("B");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().OrderBy(f => f.Name).ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object B.<P1>k__BackingField",
                "System.Object B.<P2>k__BackingField",
                "System.Object B.<P3>k__BackingField",
                "System.Object B.<P4>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(4, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        // Similar to previous test, but using backing field within accessors as well as in attributes.
        [Theory]
        [CombinatorialData]
        public void PartialProperty_Attribute_02(bool reverseOrder, bool useStatic, bool useInit)
        {
            if (useStatic && useInit) return;
            string modifier = useStatic ? "static" : "      ";
            string setter = useInit ? "init" : "set";
            string sourceA = $$"""
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                """;
            string sourceB1 = $$"""
                partial class B
                {
                    {{modifier}} partial object P1 { get; }
                    {{modifier}} partial object P2 { get; {{setter}}; }
                    {{modifier}} partial object P3 { [A(field)] get; }
                    {{modifier}} partial object P4 { get; [A(field)] {{setter}}; }
                }
                """;
            string sourceB2 = $$"""
                partial class B
                {
                    {{modifier}} partial object P1 { [A(field)] get { return field; } }
                    {{modifier}} partial object P2 { get { return null; } [A(field)] {{setter}}; }
                    {{modifier}} partial object P3 { get { return field; } }
                    {{modifier}} partial object P4 { get { return null; } {{setter}}; }
                }
                """;
            var comp = CreateCompilation(
                reverseOrder ? [sourceB2, sourceB1, sourceA] : [sourceA, sourceB1, sourceB2],
                targetFramework: GetTargetFramework(useInit));
            comp.VerifyEmitDiagnostics(
                // (3,35): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     partial        object P1 { [A(field)] get { return field; } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(3, 35),
                // (4,56): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     partial        object P2 { get { return null; } [A(field)] set; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(4, 56),
                // (5,35): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     partial        object P3 { [A(field)] get; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(5, 35),
                // (6,40): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     partial        object P4 { get; [A(field)] set; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(6, 40));

            var containingType = comp.GetMember<NamedTypeSymbol>("B");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().OrderBy(f => f.Name).ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object B.<P1>k__BackingField",
                "System.Object B.<P2>k__BackingField",
                "System.Object B.<P3>k__BackingField",
                "System.Object B.<P4>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(4, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "P4", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        [Theory]
        [CombinatorialData]
        public void PartialProperty_Attribute_03(bool reverseOrder)
        {
            string sourceA = """
                using System;
                class A : Attribute
                {
                }
                """;
            string sourceB1 = """
                partial class B
                {
                    [field: A] partial object P1 { get; set; }
                    [field: A] partial object P2 { get; set; }
                    [field: A] partial object P3 { get; set; }
                               partial object Q1 { get; set; }
                               partial object Q2 { get; set; }
                               partial object Q3 { get; set; }
                }
                """;
            string sourceB2 = """
                partial class B
                {
                               partial object P1 { get => null; set { } }
                               partial object P2 { get => field; set { } }
                               partial object P3 { get => null; set; }
                    [field: A] partial object Q1 { get => null; set { } }
                    [field: A] partial object Q2 { get => field; set { } }
                    [field: A] partial object Q3 { get => null; set; }
                }
                """;
            var comp = CreateCompilation(reverseOrder ? [sourceB2, sourceB1, sourceA] : [sourceA, sourceB1, sourceB2]);
            comp.VerifyEmitDiagnostics(
                // (3,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                //     [field: A] partial object P1 { get; set; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "property").WithLocation(3, 6),
                // (6,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'property'. All attributes in this block will be ignored.
                //     [field: A] partial object Q1 { get => null; set { } }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "property").WithLocation(6, 6));

            var containingType = comp.GetMember<NamedTypeSymbol>("B");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object B.<P2>k__BackingField",
                "System.Object B.<P3>k__BackingField",
                "System.Object B.<Q2>k__BackingField",
                "System.Object B.<Q3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(6, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });
            Assert.True(actualProperties[3] is SourcePropertySymbol { Name: "Q1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: false, BackingField: null });
            Assert.True(actualProperties[4] is SourcePropertySymbol { Name: "Q2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[5] is SourcePropertySymbol { Name: "Q3", IsPartialDefinition: true, IsAutoProperty: true, UsesFieldKeyword: false, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        // Backing field required for implementation part only (no initializer),
        // or required for both parts (with initializer).
        [Theory]
        [CombinatorialData]
        public void PartialProperty_Attribute_04(bool reverseOrder, bool includeInitializer)
        {
            string getInitializer(int value) => includeInitializer ? $"= {value};" : "";
            string sourceA = """
                using System;
                [AttributeUsage(AttributeTargets.All, AllowMultiple=true)]
                class A : Attribute
                {
                    private readonly object _obj;
                    public A(object obj) { _obj = obj; }
                    public override string ToString() => $"A({_obj})";
                }
                """;
            string sourceB1 = $$"""
                partial class B
                {
                                  partial object P1 { get; } {{getInitializer(1)}}
                    [field: A(3)] partial object P2 { get; } {{getInitializer(2)}}
                    [field: A(5)] partial object P3 { get; } {{getInitializer(3)}}
                }
                """;
            string sourceB2 = """
                partial class B
                {
                    [field: A(2)] partial object P1 { get => field; }
                                  partial object P2 { get => field; }
                    [field: A(6)] partial object P3 { get => field; }
                }
                """;
            string sourceC = """
                using System;
                using System.Reflection;
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(B).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            ReportField(field);
                    }
                    static void ReportField(FieldInfo field)
                    {
                        Console.Write("{0}:", field.Name);
                        foreach (var obj in field.GetCustomAttributes())
                            Console.Write(" {0},", obj.ToString());
                        Console.WriteLine();
                    }
                }
                """;
            var verifier = CompileAndVerify(
                reverseOrder ? [sourceC, sourceB2, sourceB1, sourceA] : [sourceA, sourceB1, sourceB2, sourceC],
                expectedOutput: """
                <P1>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(2),
                <P2>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(3),
                <P3>k__BackingField: System.Runtime.CompilerServices.CompilerGeneratedAttribute, A(5), A(6),
                """);
            verifier.VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            var containingType = comp.GetMember<NamedTypeSymbol>("B");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object B.<P1>k__BackingField",
                "System.Object B.<P2>k__BackingField",
                "System.Object B.<P3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            AssertEx.Equal(["A(2)"], actualFields[0].GetAttributes().ToStrings());
            AssertEx.Equal(["A(3)"], actualFields[1].GetAttributes().ToStrings());
            AssertEx.Equal(["A(5)", "A(6)"], actualFields[2].GetAttributes().ToStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(3, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }

        // Backing field required for definition part only.
        [Theory]
        [CombinatorialData]
        public void PartialProperty_Attribute_05(bool reverseOrder)
        {
            string sourceA = """
                using System;
                [AttributeUsage(AttributeTargets.All, AllowMultiple=true)]
                class A : Attribute
                {
                    private readonly object _obj;
                    public A(object obj) { _obj = obj; }
                    public override string ToString() => $"A({_obj})";
                }
                """;
            string sourceB1 = """
                partial class B
                {
                                  partial object P1 { [A(field)] get; }
                    [field: A(3)] partial object P2 { [A(field)] get; }
                    [field: A(5)] partial object P3 { [A(field)] get; }
                }
                """;
            string sourceB2 = """
                partial class B
                {
                    [field: A(2)] partial object P1 { get => null; }
                                  partial object P2 { get => null; }
                    [field: A(6)] partial object P3 { get => null; }
                }
                """;
            var comp = CreateCompilation(
                reverseOrder ? [sourceB2, sourceB1, sourceA] : [sourceA, sourceB1, sourceB2]);
            comp.VerifyEmitDiagnostics(
                // (3,42): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //                   partial object P1 { [A(field)] get; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(3, 42),
                // (4,42): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [field: A(3)] partial object P2 { [A(field)] get; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(4, 42),
                // (5,42): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     [field: A(5)] partial object P3 { [A(field)] get; }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(5, 42));

            var containingType = comp.GetMember<NamedTypeSymbol>("B");
            var actualFields = containingType.GetMembers().OfType<FieldSymbol>().ToImmutableArray();
            var expectedFields = new[]
            {
                "System.Object B.<P1>k__BackingField",
                "System.Object B.<P2>k__BackingField",
                "System.Object B.<P3>k__BackingField",
            };
            AssertEx.Equal(expectedFields, actualFields.ToTestDisplayStrings());

            AssertEx.Equal(["A(2)"], actualFields[0].GetAttributes().ToStrings());
            AssertEx.Equal(["A(3)"], actualFields[1].GetAttributes().ToStrings());
            AssertEx.Equal(["A(5)", "A(6)"], actualFields[2].GetAttributes().ToStrings());

            var actualProperties = containingType.GetMembers().OfType<PropertySymbol>().ToImmutableArray();
            Assert.Equal(3, actualProperties.Length);
            Assert.True(actualProperties[0] is SourcePropertySymbol { Name: "P1", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[1] is SourcePropertySymbol { Name: "P2", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });
            Assert.True(actualProperties[2] is SourcePropertySymbol { Name: "P3", IsPartialDefinition: true, IsAutoProperty: false, UsesFieldKeyword: true, BackingField: { } });

            VerifyMergedProperties(actualProperties, actualFields);
        }
    }
}
