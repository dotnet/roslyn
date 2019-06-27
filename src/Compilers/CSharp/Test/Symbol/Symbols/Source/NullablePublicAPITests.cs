// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using PublicNullableAnnotation = Microsoft.CodeAnalysis.NullableAnnotation;
using PublicNullableFlowState = Microsoft.CodeAnalysis.NullableFlowState;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public class NullablePublicAPITests : CSharpTestBase
    {
        [Fact]
        public void TestArrayNullability()
        {
            var source = @"
class C
{
    void M(C?[] arr1)
    {
        C[] arr2 = (C[])arr1;
        arr1[0].ToString();
        arr2[0].ToString();
    }
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics(
                // (6,20): warning CS8619: Nullability of reference types in value of type 'C?[]' doesn't match target type 'C[]'.
                //         C[] arr2 = (C[])arr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(C[])arr1").WithArguments("C?[]", "C[]").WithLocation(6, 20),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         arr1[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "arr1[0]").WithLocation(7, 9));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var arrayAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToList();
            var arrayTypes = arrayAccesses.Select(arr => model.GetTypeInfo(arr.Expression).Type).Cast<IArrayTypeSymbol>().ToList();

            Assert.Equal(PublicNullableAnnotation.Annotated, arrayTypes[0].ElementNullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, arrayTypes[1].ElementNullableAnnotation);
        }

        [Fact]
        public void TestTypeArgumentNullability()
        {
            var source = @"
class B<T> {}
class C
{
    B<T> M<T>(T t) where T : C? { return default!; }

    void M1(C? c)
    {
        M(new C());
        M(c);
        if (c == null) return;
        M(c);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var expressionTypes = invocations.Select(inv => model.GetTypeInfo(inv).Type).Cast<INamedTypeSymbol>().ToList();

            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[0].TypeArgumentsNullableAnnotations.Single());
            Assert.Equal(PublicNullableAnnotation.Annotated, expressionTypes[1].TypeArgumentsNullableAnnotations.Single());
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[2].TypeArgumentsNullableAnnotations.Single());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/34412")]
        public void FieldDeclarations()
        {
            var source = @"
public struct S {}
public class C
{
#nullable enable
    public C c1 = new C();
    public C? c2 = null;
    public S s1 = default;
    public S? s2 = null;

#nullable disable
    public C c3 = null;
    public C? c4 = null;
    public S s3 = default;
    public S? s4 = null;
}
";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (13,13): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //     public C? c4 = null;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 13)
                },
                new DiagnosticDescription[]
                {
                    // (5,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(5, 2),
                    // (7,13): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public C? c2 = null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(7, 13),
                    // (11,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(11, 2),
                    // (13,13): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public C? c4 = null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(13, 13)
                },
                comp =>
                {
                    var c = comp.GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IFieldSymbol>().ToArray();
                },
                member => member.NullableAnnotation,
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/34412")]
        public void PropertyDeclarations()
        {
            var source = @"
public struct S {}
public class C
{
#nullable enable
    public C C1 { get; set; } = new C();
    public C? C2 { get; set; }
    public S S1 { get; set; }
    public S? S2 { get; set; }

#nullable disable
    public C C3 { get; set; }
    public C? C4 { get; set; }
    public S S3 { get; set; }
    public S? S4 { get; set; }
}
";

            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (13,13): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //     public C? C4 { get; set; }
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 13)
                },
                new[]
                {
                    // (5,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(5, 2),
                    // (7,13): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public C? C2 { get; set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(7, 13),
                    // (11,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(11, 2),
                    // (13,13): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public C? C4 { get; set; }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(13, 13)
                },
                comp =>
                {
                    var c = comp.GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IPropertySymbol>().ToArray();
                },
                member => member.NullableAnnotation,
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/34412")]
        public void MethodReturnDeclarations()
        {
            var source = @"
public struct S {}
public class C
{
#nullable enable
    public string M0() => string.Empty;
    public string? M1() => null;
#nullable disable
    public string M2() => null;
    public string? M3() => null; // 1

#nullable enable
    public S M4() => default;
    public S? M5() => null;
#nullable disable
    public S M6() => default;
    public S? M7() => default;
}";

            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (10,18): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //     public string? M3() => null; // 1
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 18)
                },
                new[]
                {
                    // (5,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(5, 2),
                    // (7,18): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public string? M1() => null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(7, 18),
                    // (8,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(8, 2),
                    // (10,18): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public string? M3() => null; // 1
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(10, 18),
                    // (12,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(12, 2),
                    // (15,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(15, 2)
                },
                compilation =>
                {
                    var c = compilation.GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).ToArray();
                },
                member => member.ReturnNullableAnnotation,
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated);

        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/34412")]
        public void ParameterDeclarations()
        {
            var source = @"
public struct S {}
public class C
{
    public void M1(
#nullable enable
        C c1,
#nullable disable
        C c2,
#nullable enable
        C? c3,
#nullable disable
        C? c4,
#nullable enable
        S s1,
#nullable disable
        S s2,
#nullable enable
        S? s3,
#nullable disable
        S? s4) {}
}
";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (13,10): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //         C? c4,
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 10)
                },
                new[] {
                    // (6,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(6, 2),
                    // (8,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(8, 2),
                    // (10,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(10, 2),
                    // (11,10): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? c3,
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(11, 10),
                    // (12,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(12, 2),
                    // (13,10): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? c4,
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(13, 10),
                    // (14,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(14, 2),
                    // (16,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(16, 2),
                    // (18,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(18, 2),
                    // (20,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(20, 2)
                },
                compilation =>
                {
                    var c = compilation.GetTypeByMetadataName("C");
                    return c.GetMembers("M1").OfType<IMethodSymbol>().Single().Parameters.ToArray();
                },
                member => member.NullableAnnotation,
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(35034, "https://github.com/dotnet/roslyn/issues/35034")]
        public void MethodDeclarationReceiver()
        {
            var source = @"
public class C
{
#nullable enable
    public static void M1() {}
    public void M2() {}

#nullable disable
    public static void M3() {}
    public void M4() {}
}
public static class CExt
{
#nullable enable
    public static void M5(this C c) {}
    public static void M6(this C? c) {}

#nullable disable
    public static void M7(this C c) {}
    public static void M8(this C? c) {}
}
";

            var comp1 = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp1.VerifyDiagnostics(
                // (20,33): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     public static void M8(this C? c) {}
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(20, 33));
            verifyCompilation(comp1);

            var comp2 = CreateCompilation(source, options: WithNonNullTypesFalse(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp2.VerifyDiagnostics(
                // (20,33): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     public static void M8(this C? c) {}
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(20, 33));
            verifyCompilation(comp2);

            var comp3 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, skipUsesIsNullable: true);
            comp3.VerifyDiagnostics(
                // (4,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // #nullable enable
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(4, 2),
                // (8,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(8, 2),
                // (14,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // #nullable enable
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(14, 2),
                // (16,33): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void M6(this C? c) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(16, 33),
                // (18,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(18, 2),
                // (20,33): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static void M8(this C? c) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(20, 33));
            verifyCompilation(comp3);

            var comp1Emit = comp1.EmitToImageReference();
            var comp4 = CreateCompilation("", references: new[] { comp1Emit }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp4.VerifyDiagnostics();
            verifyCompilation(comp4);

            var comp2Emit = comp2.EmitToImageReference();
            var comp5 = CreateCompilation("", references: new[] { comp2Emit }, options: WithNonNullTypesFalse(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp5.VerifyDiagnostics();
            verifyCompilation(comp5);

            var comp6 = CreateCompilation("", references: new[] { comp1Emit }, parseOptions: TestOptions.Regular7_3);
            comp6.VerifyDiagnostics();
            verifyCompilation(comp6);

            void verifyCompilation(CSharpCompilation compilation)
            {
                var c = compilation.GetTypeByMetadataName("C");
                var members = c.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).ToArray();

                assertNullability(PublicNullableAnnotation.NotApplicable, 0);
                assertNullability(PublicNullableAnnotation.NotAnnotated, 1);
                assertNullability(PublicNullableAnnotation.NotApplicable, 2);
                assertNullability(PublicNullableAnnotation.NotAnnotated, 3);

                var cExt = compilation.GetTypeByMetadataName("CExt");
                members = cExt.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).Select(m => m.ReduceExtensionMethod(c)).ToArray();
                assertNullability(PublicNullableAnnotation.NotAnnotated, 0);
                assertNullability(PublicNullableAnnotation.Annotated, 1);
                assertNullability(PublicNullableAnnotation.Disabled, 2);
                assertNullability(PublicNullableAnnotation.Annotated, 3);

                void assertNullability(PublicNullableAnnotation nullability, int index)
                {
                    Assert.Equal(nullability, members[index].ReceiverNullableAnnotation);
                }
            }
        }

        [Fact]
        public void LocalFunctions()
        {
            var source = @"
#pragma warning disable CS8321 // Unused local function
public struct S {}
public class C
{
    void M1()
    {
#nullable enable
        C LM1(C c) => new C();
        C? LM2(C? c) => null;
        S LM3(S s) => default;
        S? LM4(S? s) => null;
#nullable disable
        C LM5(C c) => new C();
        C? LM6(C? c) => null;
        S LM7(S s) => default;
        S? LM8(S? s) => null;
    }
}
";

            VerifyAcrossCompilations(
                source,
                new[] {
                    // (15,10): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(15, 10),
                    // (15,17): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(15, 17)
                },
                new[] {
                    // (8,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(8, 2),
                    // (10,10): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? LM2(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(10, 10),
                    // (10,17): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? LM2(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(10, 17),
                    // (13,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(13, 2),
                    // (15,10): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(15, 10),
                    // (15,17): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(15, 17)
                },
                comp =>
                {
                    var syntaxTree = comp.SyntaxTrees[0];
                    var semanticModel = comp.GetSemanticModel(syntaxTree);
                    return syntaxTree.GetRoot().DescendantNodes().OfType<CSharp.Syntax.LocalFunctionStatementSyntax>().Select(func => semanticModel.GetDeclaredSymbol(func)).Cast<IMethodSymbol>().ToArray();
                },
                method =>
                {
                    Assert.Equal(method.ReturnNullableAnnotation, method.Parameters[0].NullableAnnotation);
                    Assert.Equal(PublicNullableAnnotation.NotApplicable, method.ReceiverNullableAnnotation);
                    return method.ReturnNullableAnnotation;
                },
                testMetadata: false,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        public void EventDeclarations()
        {
            var source = @"
#pragma warning disable CS0067 // Unused event
public class C
{
    public delegate void D();

#nullable enable
    public event D D1;
    public event D? D2;

#nullable disable
    public event D D3;
    public event D? D4;
}";
            VerifyAcrossCompilations(
                source,
                new[] { 
                    // (13,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                    //     public event D? D4;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 19)
                },
                new[] { 
                    // (7,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(7, 2),
                    // (9,19): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public event D? D2;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(9, 19),
                    // (11,2): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "nullable").WithArguments("nullable reference types").WithLocation(11, 2),
                    // (13,19): error CS8652: The feature 'nullable reference types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     public event D? D4;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "?").WithArguments("nullable reference types").WithLocation(13, 19)
                },
                compilation =>
                {
                    var c = compilation.GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IEventSymbol>().ToArray();
                },
                member => member.NullableAnnotation,
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Disabled,
                PublicNullableAnnotation.Annotated);
        }

        private static void VerifyAcrossCompilations<T>(string source,
                                                 DiagnosticDescription[] nullableEnabledErrors,
                                                 DiagnosticDescription[] nullableDisabledErrors,
                                                 Func<CSharpCompilation, T[]> memberFunc,
                                                 Func<T, PublicNullableAnnotation> nullabilityFunc,
                                                 bool testMetadata,
                                                 params PublicNullableAnnotation[] expectedNullabilities)
        {

            var comp1 = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp1.VerifyDiagnostics(nullableEnabledErrors);
            verifyCompilation(comp1);

            var comp2 = CreateCompilation(source, options: WithNonNullTypesFalse(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp2.VerifyDiagnostics(nullableEnabledErrors);
            verifyCompilation(comp2);

            var comp3 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, skipUsesIsNullable: true);
            comp3.VerifyDiagnostics(nullableDisabledErrors);
            verifyCompilation(comp3);

            if (!testMetadata) return;

            var comp1Emit = comp1.EmitToImageReference();
            var comp4 = CreateCompilation("", references: new[] { comp1Emit }, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp4.VerifyDiagnostics();
            verifyCompilation(comp4);

            var comp2Emit = comp2.EmitToImageReference();
            var comp5 = CreateCompilation("", references: new[] { comp2Emit }, options: WithNonNullTypesFalse(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp5.VerifyDiagnostics();
            verifyCompilation(comp5);

            var comp6 = CreateCompilation("", references: new[] { comp1Emit }, parseOptions: TestOptions.Regular7_3);
            comp6.VerifyDiagnostics();
            verifyCompilation(comp6);

            void verifyCompilation(CSharpCompilation compilation)
            {
                var members = memberFunc(compilation);
                AssertEx.Equal(expectedNullabilities, members.Select(nullabilityFunc));
            }
        }

        [Fact]
        public void LambdaBody_01()
        {
            var source = @"
using System;
class C
{
    void M(Action a)
    {
        M(() =>
            {
                object? o = null;
                if (o == null) return;
                o.ToString();
            });
    }
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Last();
            var typeInfo = model.GetTypeInfo(((MemberAccessExpressionSyntax)invocation.Expression).Expression);
            Assert.Equal(PublicNullableFlowState.NotNull, typeInfo.Nullability.FlowState);
            // https://github.com/dotnet/roslyn/issues/34993: This is incorrect. o should be Annotated, as you can assign
            // null without a warning.
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, typeInfo.Nullability.Annotation);
        }

        [Fact, WorkItem(34919, "https://github.com/dotnet/roslyn/issues/34919")]
        public void EnumInitializer()
        {
            var source = @"
enum E1 : byte
{
    A1
}
enum E2
{
    A2 = E1.A1
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            _ = model.GetTypeInfo(root.DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value);
        }

        [Fact]
        public void AnalyzerTest()
        {
            var source = @"
class C
{
    void M()
    {
        object? o = null;
        _ = o;
        if (o == null) return;
        _ = o;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);

            comp.VerifyDiagnostics();
            comp.VerifyAnalyzerDiagnostics(new[] { new NullabilityPrinter() }, null, null, true,
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "MaybeNull", "Annotated", "MaybeNull").WithLocation(7, 13),
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "MaybeNull", "Annotated", "MaybeNull").WithLocation(8, 13),
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "NotNull", "NotAnnotated", "NotNull").WithLocation(9, 13));
        }

        private class NullabilityPrinter : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor);

            private static DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(id: "CA9999_NullabilityPrinter", title: "CA9999_NullabilityPrinter", messageFormat: "Nullability of '{0}' is '{1}':'{2}'. Speculative flow state is '{3}'", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override void Initialize(AnalysisContext context)
            {
                var newSource = (ExpressionStatementSyntax)SyntaxFactory.ParseStatement("_ = o;");
                var oReference = ((AssignmentExpressionSyntax)newSource.Expression).Right;

                context.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    if (syntaxContext.Node.ToString() == "_") return;
                    var info = syntaxContext.SemanticModel.GetTypeInfo(syntaxContext.Node);
                    Assert.True(syntaxContext.SemanticModel.TryGetSpeculativeSemanticModel(syntaxContext.Node.SpanStart, newSource, out var specModel));
                    var specInfo = specModel.GetTypeInfo(oReference);
                    syntaxContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_descriptor, syntaxContext.Node.GetLocation(), syntaxContext.Node, info.Nullability.FlowState, info.Nullability.Annotation, specInfo.Nullability.FlowState));
                }, SyntaxKind.IdentifierName);
            }
        }

        [Fact]
        public void MultipleConversions()
        {
            var source = @"
class A { public static explicit operator C(A a) => new D(); }
class B : A { }
class C { }
class D : C { }
class E
{
    void M()
    {
        var d = (D)(C?)new B();
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics(
                // (10,17): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         var d = (D)(C?)new B();
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "(D)(C?)new B()").WithLocation(10, 17));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var aType = comp.GetTypeByMetadataName("A");
            var bType = comp.GetTypeByMetadataName("B");
            var cType = comp.GetTypeByMetadataName("C");
            var dType = comp.GetTypeByMetadataName("D");

            var nullable = new NullabilityInfo(PublicNullableAnnotation.Annotated, PublicNullableFlowState.MaybeNull);
            var notNullable = new NullabilityInfo(PublicNullableAnnotation.NotAnnotated, PublicNullableFlowState.NotNull);

            var dCast = (CastExpressionSyntax)root.DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;
            var dInfo = model.GetTypeInfo(dCast);
            Assert.Equal(dType, dInfo.Type);
            Assert.Equal(dType, dInfo.ConvertedType);
            Assert.Equal(nullable, dInfo.Nullability);
            Assert.Equal(nullable, dInfo.ConvertedNullability);

            var cCast = (CastExpressionSyntax)dCast.Expression;
            var cInfo = model.GetTypeInfo(cCast);
            Assert.Equal(cType, cInfo.Type);
            Assert.Equal(cType, cInfo.ConvertedType);
            Assert.Equal(nullable, cInfo.Nullability);
            Assert.Equal(nullable, cInfo.ConvertedNullability);

            var objectCreation = cCast.Expression;
            var creationInfo = model.GetTypeInfo(objectCreation);
            Assert.Equal(bType, creationInfo.Type);
            Assert.Equal(aType, creationInfo.ConvertedType);
            Assert.Equal(notNullable, creationInfo.Nullability);
            Assert.Equal(nullable, creationInfo.ConvertedNullability);
        }

        [Fact]
        public void ConditionalOperator_InvalidType()
        {
            var source = @"
class C
{
    void M()
    {
        var x = new Undefined() ? new object() : null;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics(
                // (6,21): error CS0246: The type or namespace name 'Undefined' could not be found (are you missing a using directive or an assembly reference?)
                //         var x = new Undefined() ? new object() : null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Undefined").WithArguments("Undefined").WithLocation(6, 21));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var conditional = root.DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();

            var notNull = new NullabilityInfo(PublicNullableAnnotation.NotAnnotated, PublicNullableFlowState.NotNull);
            var @null = new NullabilityInfo(PublicNullableAnnotation.Annotated, PublicNullableFlowState.MaybeNull);

            var leftInfo = model.GetTypeInfo(conditional.WhenTrue);
            var rightInfo = model.GetTypeInfo(conditional.WhenFalse);

            Assert.Equal(notNull, leftInfo.Nullability);
            Assert.Equal(notNull, leftInfo.ConvertedNullability);
            Assert.Equal(@null, rightInfo.Nullability);
            Assert.Equal(notNull, rightInfo.ConvertedNullability);
        }

        [Fact]
        public void InferredDeclarationType()
        {
            var source =
@"
using System.Collections.Generic;
#nullable enable
class C : System.IDisposable
{
    void M(C? x, C x2)
    {
        var /*T:C?*/ y = x;
        var /*T:C!*/ y2 = x2;
        
        using var /*T:C?*/ y3 = x;
        using var /*T:C!*/ y4 = x2;
        
        using (var /*T:C?*/ y5 = x) { }
        using (var /*T:C!*/ y6 = x2) { }
        
        ref var/*T:C?*/ y7 = ref x;
        ref var/*T:C!*/ y8 = ref x2;
        
        if (x == null) 
            return;
        var /*T:C!*/ y9 = x;
        using var /*T:C!*/ y10 = x;
        ref var /*T:C!*/ y11 = ref x;
        
        x = null;
        var /*T:C?*/ y12 = x;
        using var /*T:C?*/ y13 = x;
        ref var /*T:C?*/ y14 = ref x;
        
        x2 = null; // 1
        var /*T:C?*/ y15 = x2;
        using var /*T:C?*/ y16 = x2;
        ref var /*T:C?*/ y17 = ref x2;
    }
    
    void M2(List<C?> l1, List<C> l2)
    {
        foreach (var /*T:C?*/ x in l1) { }
        foreach (var /*T:C!*/ x in l2) { }
    }

    public void Dispose() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (31,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x2 = null; // 1
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(31, 14)
                );
            comp.VerifyTypes();
        }

        [Fact]
        public void SpeculativeSemanticModel_BasicTest()
        {
            var source = @"
class C
{
    void M(string? s1)
    {
        if (s1 != null)
        {
            s1.ToString();
        }

        s1?.ToString();

        s1 = """";
        var s2 = s1 == null ? """" : s1;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var ifStatement = root.DescendantNodes().OfType<IfStatementSyntax>().Single();
            var conditionalAccessExpression = root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single();
            var ternary = root.DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();

            var newSource = ((ExpressionStatementSyntax)SyntaxFactory.ParseStatement(@"_ = s1 == """" ? s1 : s1;"));
            var newTernary = (ConditionalExpressionSyntax)((AssignmentExpressionSyntax)newSource.Expression).Right;
            var inCondition = ((BinaryExpressionSyntax)newTernary.Condition).Left;
            var whenTrue = newTernary.WhenTrue;
            var whenFalse = newTernary.WhenFalse;

            // Before the if statement
            verifySpeculativeModel(ifStatement.SpanStart, PublicNullableFlowState.MaybeNull);

            // In if statement consequence
            verifySpeculativeModel(ifStatement.Statement.SpanStart, PublicNullableFlowState.NotNull);

            // Before the conditional access
            verifySpeculativeModel(conditionalAccessExpression.SpanStart, PublicNullableFlowState.MaybeNull);

            // After the conditional access
            verifySpeculativeModel(conditionalAccessExpression.WhenNotNull.SpanStart, PublicNullableFlowState.NotNull);

            // In the conditional whenTrue
            verifySpeculativeModel(ternary.WhenTrue.SpanStart, PublicNullableFlowState.MaybeNull);

            // In the conditional whenFalse
            verifySpeculativeModel(ternary.WhenFalse.SpanStart, PublicNullableFlowState.NotNull);

            void verifySpeculativeModel(int spanStart, PublicNullableFlowState conditionFlowState)
            {
                Assert.True(model.TryGetSpeculativeSemanticModel(spanStart, newSource, out var speculativeModel));
                var speculativeTypeInfo = speculativeModel.GetTypeInfo(inCondition);
                Assert.Equal(conditionFlowState, speculativeTypeInfo.Nullability.FlowState);
                speculativeTypeInfo = speculativeModel.GetTypeInfo(whenTrue);
                Assert.Equal(PublicNullableFlowState.NotNull, speculativeTypeInfo.Nullability.FlowState);
                speculativeTypeInfo = speculativeModel.GetTypeInfo(whenFalse);
                Assert.Equal(conditionFlowState, speculativeTypeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        public void SpeculativeModel_Properties()
        {
            var source = @"
class C
{
    object? Foo
    {
        get
        {
            object? x = null;
            return x;
        }
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: TestOptions.Regular8WithNullableAnalysis);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var returnStatement = root.DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            var newSource = (BlockSyntax)SyntaxFactory.ParseStatement("{ var y = x ?? new object(); y.ToString(); }");
            var yReference = ((MemberAccessExpressionSyntax)newSource.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression).Expression;
            Assert.True(model.TryGetSpeculativeSemanticModel(returnStatement.SpanStart, newSource, out var specModel));
            var speculativeTypeInfo = specModel.GetTypeInfo(yReference);
            Assert.Equal(PublicNullableFlowState.NotNull, speculativeTypeInfo.Nullability.FlowState);
        }

        [Fact]
        public void TupleAssignment()
        {
            var source =
@"
#pragma warning disable CS0219
#nullable enable
class C
{
    void M(C? x, C x2)
    {
        (C? a, C b) t = (x, x2) /*T:(C? x, C! x2)*/ /*CT:(C? a, C! b)*/;
        (object a, int b) t2 = (x, (short)0)/*T:(C? x, short)*/ /*CT:(object! a, int b)*/; // 1
        (object a, int b) t3 = (default, default) /*T:<null>!*/ /*CT:(object! a, int b)*/; // 2
        (object a, int b) t4 = (default(object), default(int)) /*T:(object?, int)*/ /*CT:(object! a, int b)*/; // 3
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyTypes();
            comp.VerifyDiagnostics(
                // (9,32): warning CS8619: Nullability of reference types in value of type '(object? x, int)' doesn't match target type '(object a, int b)'.
                //         (object a, int b) t2 = (x, (short)0)/*T:(C? x, short)*/ /*CT:(object! a, int b)*/; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(x, (short)0)").WithArguments("(object? x, int)", "(object a, int b)").WithLocation(9, 32),
                // (10,32): warning CS8619: Nullability of reference types in value of type '(object?, int)' doesn't match target type '(object a, int b)'.
                //         (object a, int b) t3 = (default, default) /*T:<null>!*/ /*CT:(object! a, int b)*/; //2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(default, default)").WithArguments("(object?, int)", "(object a, int b)").WithLocation(10, 32),
                // (11,32): warning CS8619: Nullability of reference types in value of type '(object?, int)' doesn't match target type '(object a, int b)'.
                //         (object a, int b) t4 = (default(object), default(int)) /*T:(object?, int)*/ /*CT:(object! a, int b)*/; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(default(object), default(int))").WithArguments("(object?, int)", "(object a, int b)").WithLocation(11, 32)
                );
        }
    }
}
