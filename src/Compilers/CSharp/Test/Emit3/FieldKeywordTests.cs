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

        // PROTOTYPE: Confirm we want to allow mixed auto- and explicitly-implemented
        // accessors when explicitly-implemented accessors do not use 'field'.
        [Fact]
        public void Initializer_01()
        {
            string source = """
                using System;
                class C
                {
                    public static int P1 { get; } = 1;
                    public static int P2 { get => field; } = 2;
                    public static int P3 { get => field; set; } = 3;
                    public static int P5 { get => field; set { } } = 5;
                    public static int P7 { get => 0; set; } = 7;
                    public static int P9 { get; set; } = 9;
                    public static int PB { get; set { } } = 11;
                    public static int PD { set { field = value; } } = 13;
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P5, C.P7, C.P9, C.PB));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2, 3, 5, 0, 9, 11)");
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       52 (0x34)
                  .maxstack  1
                  IL_0000:  ldc.i4.1
                  IL_0001:  stsfld     "int C.<P1>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  stsfld     "int C.<P2>k__BackingField"
                  IL_000c:  ldc.i4.3
                  IL_000d:  stsfld     "int C.<P3>k__BackingField"
                  IL_0012:  ldc.i4.5
                  IL_0013:  stsfld     "int C.<P5>k__BackingField"
                  IL_0018:  ldc.i4.7
                  IL_0019:  stsfld     "int C.<P7>k__BackingField"
                  IL_001e:  ldc.i4.s   9
                  IL_0020:  stsfld     "int C.<P9>k__BackingField"
                  IL_0025:  ldc.i4.s   11
                  IL_0027:  stsfld     "int C.<PB>k__BackingField"
                  IL_002c:  ldc.i4.s   13
                  IL_002e:  stsfld     "int C.<PD>k__BackingField"
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void Initializer_02()
        {
            string source = """
                using System;
                class C
                {
                    public int P1 { get; } = 1;
                    public int P2 { get => field; } = 2;
                    public int P3 { get => field; set; } = 3;
                    public int P4 { get => field; init; } = 4;
                    public int P5 { get => field; set { } } = 5;
                    public int P6 { get => field; init { } } = 6;
                    public int P7 { get => 0; set; } = 7;
                    public int P8 { get => 0; init; } = 8;
                    public int P9 { get; set; } = 9;
                    public int PA { get; init; } = 10;
                    public int PB { get; set { } } = 11;
                    public int PC { get; init { } } = 12;
                    public int PD { set { field = value; } } = 13;
                    public int PE { init { field = value; } } = 14;
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2, c.P3, c.P4, c.P5, c.P6, c.P7, c.P8, c.P9, c.PA, c.PB, c.PC));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net80, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("(1, 2, 3, 4, 5, 6, 0, 0, 9, 10, 11, 12)"));
            verifier.VerifyIL("C..ctor", """
                {
                  // Code size      111 (0x6f)
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
                  IL_0041:  ldc.i4.s   10
                  IL_0043:  stfld      "int C.<PA>k__BackingField"
                  IL_0048:  ldarg.0
                  IL_0049:  ldc.i4.s   11
                  IL_004b:  stfld      "int C.<PB>k__BackingField"
                  IL_0050:  ldarg.0
                  IL_0051:  ldc.i4.s   12
                  IL_0053:  stfld      "int C.<PC>k__BackingField"
                  IL_0058:  ldarg.0
                  IL_0059:  ldc.i4.s   13
                  IL_005b:  stfld      "int C.<PD>k__BackingField"
                  IL_0060:  ldarg.0
                  IL_0061:  ldc.i4.s   14
                  IL_0063:  stfld      "int C.<PE>k__BackingField"
                  IL_0068:  ldarg.0
                  IL_0069:  call       "object..ctor()"
                  IL_006e:  ret
                }
                """);
        }

        [Fact]
        public void Initializer_03()
        {
            string source = """
                class C
                {
                    public static int P1 { get => 0; } = 1;
                    public static int P2 { get => 0; set { } } = 2;
                    public int P3 { get => 0; } = 3;
                    public int P4 { get => 0; set { } } = 4;
                    public int P5 { get => 0; init { } } = 5;
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,23): error CS8050: Only auto-implemented properties can have initializers.
                //     public static int P1 { get => 0; } = 1;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P1").WithLocation(3, 23),
                // (4,23): error CS8050: Only auto-implemented properties can have initializers.
                //     public static int P2 { get => 0; set { } } = 2;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P2").WithLocation(4, 23),
                // (5,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int P3 { get => 0; } = 3;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P3").WithLocation(5, 16),
                // (6,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int P4 { get => 0; set { } } = 4;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P4").WithLocation(6, 16),
                // (7,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int P5 { get => 0; init { } } = 5;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P5").WithLocation(7, 16));
        }

        [Fact]
        public void ConstructorAssignment_01()
        {
            string source = """
                using System;
                class C
                {
                    public static int P1 { get; }
                    public static int P2 { get => field; }
                    public static int P3 { get => field; set; }
                    public static int P5 { get => field; set { } }
                    public static int P7 { get => 0; set; }
                    public static int P9 { get; set; }
                    public static int PB { get; set { } }
                    public static int PD { set { field = value; } }
                    static C()
                    {
                        P1 = 1;
                        P2 = 2;
                        P3 = 3;
                        P5 = 5;
                        P7 = 7;
                        P9 = 9;
                        PB = 11;
                        PD = 13;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine((C.P1, C.P2, C.P3, C.P5, C.P7, C.P9, C.PB));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2, 3, 0, 0, 9, 0)");
            verifier.VerifyIL("C..cctor", """
                {
                  // Code size       52 (0x34)
                  .maxstack  1
                  IL_0000:  ldc.i4.1
                  IL_0001:  stsfld     "int C.<P1>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  stsfld     "int C.<P2>k__BackingField"
                  IL_000c:  ldc.i4.3
                  IL_000d:  call       "void C.P3.set"
                  IL_0012:  ldc.i4.5
                  IL_0013:  call       "void C.P5.set"
                  IL_0018:  ldc.i4.7
                  IL_0019:  call       "void C.P7.set"
                  IL_001e:  ldc.i4.s   9
                  IL_0020:  call       "void C.P9.set"
                  IL_0025:  ldc.i4.s   11
                  IL_0027:  call       "void C.PB.set"
                  IL_002c:  ldc.i4.s   13
                  IL_002e:  call       "void C.PD.set"
                  IL_0033:  ret
                }
                """);
        }

        [Fact]
        public void ConstructorAssignment_02()
        {
            string source = """
                using System;
                class C
                {
                    public int P1 { get; }
                    public int P2 { get => field; }
                    public int P3 { get => field; set; }
                    public int P4 { get => field; init; }
                    public int P5 { get => field; set { } }
                    public int P6 { get => field; init { } }
                    public int P7 { get => 0; set; }
                    public int P8 { get => 0; init; }
                    public int P9 { get; set; }
                    public int PA { get; init; }
                    public int PB { get; set { } }
                    public int PC { get; init { } }
                    public int PD { set { field = value; } }
                    public int PE { init { field = value; } }
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
                        PA = 10;
                        PB = 11;
                        PC = 12;
                        PD = 13;
                        PE = 14;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P1, c.P2, c.P3, c.P4, c.P5, c.P6, c.P7, c.P8, c.P9, c.PA, c.PB, c.PC));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, targetFramework: TargetFramework.Net80, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("(1, 2, 3, 4, 0, 0, 0, 0, 9, 10, 0, 0)"));
            verifier.VerifyIL("C..ctor", """
                {
                  // Code size      111 (0x6f)
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
                  IL_0016:  call       "void C.P3.set"
                  IL_001b:  ldarg.0
                  IL_001c:  ldc.i4.4
                  IL_001d:  call       "void C.P4.init"
                  IL_0022:  ldarg.0
                  IL_0023:  ldc.i4.5
                  IL_0024:  call       "void C.P5.set"
                  IL_0029:  ldarg.0
                  IL_002a:  ldc.i4.6
                  IL_002b:  call       "void C.P6.init"
                  IL_0030:  ldarg.0
                  IL_0031:  ldc.i4.7
                  IL_0032:  call       "void C.P7.set"
                  IL_0037:  ldarg.0
                  IL_0038:  ldc.i4.8
                  IL_0039:  call       "void C.P8.init"
                  IL_003e:  ldarg.0
                  IL_003f:  ldc.i4.s   9
                  IL_0041:  call       "void C.P9.set"
                  IL_0046:  ldarg.0
                  IL_0047:  ldc.i4.s   10
                  IL_0049:  call       "void C.PA.init"
                  IL_004e:  ldarg.0
                  IL_004f:  ldc.i4.s   11
                  IL_0051:  call       "void C.PB.set"
                  IL_0056:  ldarg.0
                  IL_0057:  ldc.i4.s   12
                  IL_0059:  call       "void C.PC.init"
                  IL_005e:  ldarg.0
                  IL_005f:  ldc.i4.s   13
                  IL_0061:  call       "void C.PD.set"
                  IL_0066:  ldarg.0
                  IL_0067:  ldc.i4.s   14
                  IL_0069:  call       "void C.PE.init"
                  IL_006e:  ret
                }
                """);
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
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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

        [Theory]
        [CombinatorialData]
        public void ReadOnly_01(bool useReadOnlyType, bool useReadOnlyProperty)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string propertyModifier = getReadOnlyModifier(useReadOnlyProperty);
            string source = $$"""
                {{typeModifier}} struct S
                {
                    {{propertyModifier}} object P1 { get; }
                    {{propertyModifier}} object P2 { get => field; }
                    {{propertyModifier}} object P3 { get => field; set; }
                    {{propertyModifier}} object P4 { get => field; init; }
                    {{propertyModifier}} object P5 { get => field; set { } }
                    {{propertyModifier}} object P6 { get => field; init { } }
                    {{propertyModifier}} object P7 { get => null; }
                    {{propertyModifier}} object P8 { get => null; set; }
                    {{propertyModifier}} object P9 { get => null; init; }
                    {{propertyModifier}} object PA { get => null; set { } }
                    {{propertyModifier}} object PB { get => null; init { } }
                    {{propertyModifier}} object PC { get => null; set { _ = field; } }
                    {{propertyModifier}} object PD { get => null; init { _ = field; } }
                    {{propertyModifier}} object PE { get; set; }
                    {{propertyModifier}} object PF { get; init; }
                    {{propertyModifier}} object PG { get; set { } }
                    {{propertyModifier}} object PH { get; init { } }
                    {{propertyModifier}} object PI { set { _ = field; } }
                    {{propertyModifier}} object PJ { init { _ = field; } }
                    {{propertyModifier}} object PK { get { field = null; return null; } }
                    {{propertyModifier}} object PL { get; set { field = value; } }
                    {{propertyModifier}} object PM { get; init { field = value; } }
                    {{propertyModifier}} object PN { set { field = value; } }
                    {{propertyModifier}} object PO { init { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            if (useReadOnlyType)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object P3 { get => field; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P3").WithLocation(5, 21),
                    // (10,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object P8 { get => null; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P8").WithLocation(10, 21),
                    // (16,21): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                    //              object PE { get; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "PE").WithLocation(16, 21),
                    // (22,32): error CS1604: Cannot assign to 'field' because it is read-only
                    //              object PK { get { field = null; return null; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(22, 32),
                    // (23,37): error CS1604: Cannot assign to 'field' because it is read-only
                    //              object PL { get; set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(23, 37),
                    // (25,32): error CS1604: Cannot assign to 'field' because it is read-only
                    //              object PN { set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(25, 32));
            }
            else if (useReadOnlyProperty)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,21): error CS8659: Auto-implemented property 'S.P3' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object P3 { get => field; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P3").WithArguments("S.P3").WithLocation(5, 21),
                    // (10,21): error CS8659: Auto-implemented property 'S.P8' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object P8 { get => null; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P8").WithArguments("S.P8").WithLocation(10, 21),
                    // (16,21): error CS8659: Auto-implemented property 'S.PE' cannot be marked 'readonly' because it has a 'set' accessor.
                    //     readonly object PE { get; set; }
                    Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "PE").WithArguments("S.PE").WithLocation(16, 21),
                    // (22,32): error CS1604: Cannot assign to 'field' because it is read-only
                    //     readonly object PK { get { field = null; return null; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(22, 32),
                    // (23,37): error CS1604: Cannot assign to 'field' because it is read-only
                    //     readonly object PL { get; set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(23, 37),
                    // (25,32): error CS1604: Cannot assign to 'field' because it is read-only
                    //     readonly object PN { set { field = value; } }
                    Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(25, 32));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
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
                    object P5 { {{getModifier}} get => field; {{setModifier}} set { } }
                    object P8 { {{getModifier}} get => null; {{setModifier}} set; }
                    object PA { {{getModifier}} get => null; {{setModifier}} set { } }
                    object PC { {{getModifier}} get => null; {{setModifier}} set { _ = field; } }
                    object PE { {{getModifier}} get; {{setModifier}} set; }
                    object PG { {{getModifier}} get; {{setModifier}} set { } }
                    object PK { {{getModifier}} get { field = null; return null; } set { } }
                    object PL { {{getModifier}} get; {{setModifier}} set { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            if (useReadOnlyType)
            {
                if (useReadOnlyOnGet)
                {
                    comp.VerifyEmitDiagnostics(
                        // (3,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P3 { readonly get => field;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P3").WithLocation(3, 12),
                        // (5,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object P8 { readonly get => null;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P8").WithLocation(5, 12),
                        // (8,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object PE { readonly get;          set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "PE").WithLocation(8, 12),
                        // (10,32): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PK { readonly get { field = null; return null; } set { } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(10, 32),
                        // (11,46): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PL { readonly get;          set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(11, 46));
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
                        //     object P8 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "P8").WithLocation(5, 12),
                        // (5,48): error CS8658: Auto-implemented 'set' accessor 'S.P8.set' cannot be marked 'readonly'.
                        //     object P8 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P8.set").WithLocation(5, 48),
                        // (8,12): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                        //     object PE {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "PE").WithLocation(8, 12),
                        // (8,40): error CS8658: Auto-implemented 'set' accessor 'S.PE.set' cannot be marked 'readonly'.
                        //     object PE {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.PE.set").WithLocation(8, 40),
                        // (10,32): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PK {          get { field = null; return null; } set { } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(10, 32),
                        // (11,46): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PL {          get; readonly set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(11, 46));
                }
            }
            else
            {
                if (useReadOnlyOnGet)
                {
                    comp.VerifyEmitDiagnostics(
                        // (10,32): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PK { readonly get { field = null; return null; } set { } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(10, 32));
                }
                else
                {
                    comp.VerifyEmitDiagnostics(
                        // (3,49): error CS8658: Auto-implemented 'set' accessor 'S.P3.set' cannot be marked 'readonly'.
                        //     object P3 {          get => field; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P3.set").WithLocation(3, 49),
                        // (5,48): error CS8658: Auto-implemented 'set' accessor 'S.P8.set' cannot be marked 'readonly'.
                        //     object P8 {          get => null; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P8.set").WithLocation(5, 48),
                        // (8,40): error CS8658: Auto-implemented 'set' accessor 'S.PE.set' cannot be marked 'readonly'.
                        //     object PE {          get; readonly set; }
                        Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.PE.set").WithLocation(8, 40),
                        // (11,46): error CS1604: Cannot assign to 'field' because it is read-only
                        //     object PL {          get; readonly set { field = value; } }
                        Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "field").WithArguments("field").WithLocation(11, 46));
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_03(bool useReadOnlyType, bool useReadOnlyProperty)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string propertyModifier = getReadOnlyModifier(useReadOnlyProperty);
            string source = $$"""
                {{typeModifier}} struct S
                {
                    static {{propertyModifier}} object P1 { get; }
                    static {{propertyModifier}} object P2 { get => field; }
                    static {{propertyModifier}} object P3 { get => field; set; }
                    static {{propertyModifier}} object PE { get; set; }
                    static {{propertyModifier}} object PG { get; set { } }
                    static {{propertyModifier}} object PL { get; set { field = value; } }
                    static {{propertyModifier}} object PN { set { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            if (useReadOnlyProperty)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,28): error CS8657: Static member 'S.P1' cannot be marked 'readonly'.
                    //     static readonly object P1 { get; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P1").WithArguments("S.P1").WithLocation(3, 28),
                    // (4,28): error CS8657: Static member 'S.P2' cannot be marked 'readonly'.
                    //     static readonly object P2 { get => field; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P2").WithArguments("S.P2").WithLocation(4, 28),
                    // (5,28): error CS8657: Static member 'S.P3' cannot be marked 'readonly'.
                    //     static readonly object P3 { get => field; set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P3").WithArguments("S.P3").WithLocation(5, 28),
                    // (6,28): error CS8657: Static member 'S.PE' cannot be marked 'readonly'.
                    //     static readonly object PE { get; set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "PE").WithArguments("S.PE").WithLocation(6, 28),
                    // (7,28): error CS8657: Static member 'S.PG' cannot be marked 'readonly'.
                    //     static readonly object PG { get; set { } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "PG").WithArguments("S.PG").WithLocation(7, 28),
                    // (8,28): error CS8657: Static member 'S.PL' cannot be marked 'readonly'.
                    //     static readonly object PL { get; set { field = value; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "PL").WithArguments("S.PL").WithLocation(8, 28),
                    // (9,28): error CS8657: Static member 'S.PN' cannot be marked 'readonly'.
                    //     static readonly object PN { set { field = value; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "PN").WithArguments("S.PN").WithLocation(9, 28));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void ReadOnly_04(bool useReadOnlyType, bool useReadOnlyOnGet)
        {
            static string getReadOnlyModifier(bool useReadOnly) => useReadOnly ? "readonly" : "        ";
            string typeModifier = getReadOnlyModifier(useReadOnlyType);
            string getModifier = getReadOnlyModifier(useReadOnlyOnGet);
            string setModifier = getReadOnlyModifier(!useReadOnlyOnGet);
            string source = $$"""
                {{typeModifier}} struct S
                {
                    static object P3 { {{getModifier}} get => field; {{setModifier}} set; }
                    static object PE { {{getModifier}} get; {{setModifier}} set; }
                    static object PL { {{getModifier}} get; {{setModifier}} set { field = value; } }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            if (useReadOnlyOnGet)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,33): error CS8657: Static member 'S.P3.get' cannot be marked 'readonly'.
                    //     static object P3 { readonly get => field;          set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.P3.get").WithLocation(3, 33),
                    // (4,33): error CS8657: Static member 'S.PE.get' cannot be marked 'readonly'.
                    //     static object PE { readonly get;          set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.PE.get").WithLocation(4, 33),
                    // (5,33): error CS8657: Static member 'S.PL.get' cannot be marked 'readonly'.
                    //     static object PL { readonly get;          set { field = value; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.PL.get").WithLocation(5, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,56): error CS8657: Static member 'S.P3.set' cannot be marked 'readonly'.
                    //     static object P3 {          get => field; readonly set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "set").WithArguments("S.P3.set").WithLocation(3, 56),
                    // (4,47): error CS8657: Static member 'S.PE.set' cannot be marked 'readonly'.
                    //     static object PE {          get; readonly set; }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "set").WithArguments("S.PE.set").WithLocation(4, 47),
                    // (5,47): error CS8657: Static member 'S.PL.set' cannot be marked 'readonly'.
                    //     static object PL {          get; readonly set { field = value; } }
                    Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "set").WithArguments("S.PL.set").WithLocation(5, 47));
            }
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
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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

        // PROTOTYPE: Confirm that both accessors must be overridden.
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
            var comp = CreateCompilation([sourceA, sourceB0], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB1], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB3], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB4], targetFramework: TargetFramework.Net80);
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
            var comp = CreateCompilation([sourceA, sourceB0], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB1], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB2], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB3], targetFramework: TargetFramework.Net80);
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
            comp = CreateCompilation([sourceA, sourceB4], targetFramework: TargetFramework.Net80);
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
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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
    }
}
