// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

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
                <Q>k__BackingField: False
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
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { get => field; } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
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
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,28): error CS0103: The name 'field' does not exist in the current context
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 28));
        }

        [Theory]
        [CombinatorialData]
        public void ImplicitAccessorBody_01(
            [CombinatorialValues("class", "struct", "ref struct", "record", "record struct")] string typeKind,
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.Preview)] LanguageVersion languageVersion)
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
                #pragma warning disable 9258 // 'field' is a contextual keyword
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
                // (18,25): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                //     public object P4 { [field: A(4)] get; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return").WithLocation(18, 25),
                // (19,37): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //     public static object P5 { get; [field: A(5)] set; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(19, 37),
                // (23,25): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, return'. All attributes in this block will be ignored.
                //     public object Q4 { [field: A(4)] get => field; }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, return").WithLocation(23, 25),
                // (24,54): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'method, param, return'. All attributes in this block will be ignored.
                //     public static object Q5 { get { return field; } [field: A(5)] set { } }
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "method, param, return").WithLocation(24, 54));

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

        [Fact]
        public void RestrictedTypes()
        {
            string source = """
                #pragma warning disable 9258 // 'field' is a contextual keyword
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
                #pragma warning disable 9258 // 'field' is a contextual keyword
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
                #pragma warning disable 9258 // 'field' is a contextual keyword
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
                #pragma warning disable 9258 // 'field' is a contextual keyword
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
    }
}
