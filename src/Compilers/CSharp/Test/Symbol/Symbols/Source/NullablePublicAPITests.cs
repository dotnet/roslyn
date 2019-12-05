// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
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
            Assert.Equal(PublicNullableAnnotation.Annotated, arrayTypes[0].ElementType.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, arrayTypes[1].ElementNullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, arrayTypes[1].ElementType.NullableAnnotation);
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var expressionTypes = invocations.Select(inv => model.GetTypeInfo(inv).Type).Cast<INamedTypeSymbol>().ToList();

            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[0].TypeArgumentNullableAnnotations.Single());
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[0].TypeArgumentNullableAnnotations().Single());
            Assert.Equal(PublicNullableAnnotation.Annotated, expressionTypes[1].TypeArgumentNullableAnnotations.Single());
            Assert.Equal(PublicNullableAnnotation.Annotated, expressionTypes[1].TypeArgumentNullableAnnotations().Single());
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[2].TypeArgumentNullableAnnotations.Single());
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, expressionTypes[2].TypeArgumentNullableAnnotations().Single());
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
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
                    // (5,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(5, 2),
                    // (7,13): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public C? c2 = null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(7, 13),
                    // (11,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(11, 2),
                    // (13,13): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public C? c4 = null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(13, 13)
                },
                comp =>
                {
                    var c = ((Compilation)comp).GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IFieldSymbol>().ToArray();
                },
                member =>
                {
                    var result = member.Type.NullableAnnotation;
                    Assert.Equal(result, member.NullableAnnotation);
                    return member.Type.NullableAnnotation;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
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
                    // (5,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(5, 2),
                    // (7,13): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public C? C2 { get; set; }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(7, 13),
                    // (11,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(11, 2),
                    // (13,13): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public C? C4 { get; set; }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(13, 13)
                },
                comp =>
                {
                    var c = ((Compilation)comp).GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IPropertySymbol>().ToArray();
                },
                member =>
                {
                    var result = member.Type.NullableAnnotation;
                    Assert.Equal(result, member.NullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
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
                    // (5,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(5, 2),
                    // (7,18): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public string? M1() => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(7, 18),
                    // (8,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(8, 2),
                    // (10,18): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public string? M3() => null; // 1
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(10, 18),
                    // (12,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(12, 2),
                    // (15,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(15, 2)
                },
                compilation =>
                {
                    var c = ((Compilation)compilation).GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).ToArray();
                },
                member =>
                {
                    var result = member.ReturnType.NullableAnnotation;
                    Assert.Equal(result, member.ReturnNullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
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
                    // (6,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(6, 2),
                    // (8,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(8, 2),
                    // (10,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(10, 2),
                    // (11,10): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? c3,
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(11, 10),
                    // (12,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(12, 2),
                    // (13,10): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? c4,
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(13, 10),
                    // (14,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(14, 2),
                    // (16,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(16, 2),
                    // (18,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(18, 2),
                    // (20,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(20, 2)
                },
                compilation =>
                {
                    var c = ((Compilation)compilation).GetTypeByMetadataName("C");
                    return c.GetMembers("M1").OfType<IMethodSymbol>().Single().Parameters.ToArray();
                },
                member =>
                {
                    var result = member.Type.NullableAnnotation;
                    Assert.Equal(result, member.NullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.NotAnnotated,
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
public static class Ext
{
#nullable enable
    public static void M5(this C c) {}
    public static void M6(this C? c) {}
    public static void M7(this int i) {}
    public static void M8(this int? i) {}

#nullable disable
    public static void M9(this C c) {}
    public static void M10(this C? c) {}
    public static void M11(this int i) {}
    public static void M12(this int? i) {}
}
";

            var comp1 = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp1.VerifyDiagnostics(
                // (22,34): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static void M10(this C? c) {}
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(22, 34));
            verifyCompilation(comp1);

            var comp2 = CreateCompilation(source, options: WithNonNullTypesFalse());
            comp2.VerifyDiagnostics(
                // (22,34): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public static void M10(this C? c) {}
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(22, 34));
            verifyCompilation(comp2);

            var comp3 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, skipUsesIsNullable: true);
            comp3.VerifyDiagnostics(
                // (4,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable enable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(4, 2),
                // (8,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(8, 2),
                // (14,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable enable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(14, 2),
                // (16,33): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public static void M6(this C? c) {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(16, 33),
                // (20,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // #nullable disable
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(20, 2),
                // (22,34): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public static void M10(this C? c) {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(22, 34));
            verifyCompilation(comp3);

            var comp1Emit = comp1.EmitToImageReference();
            var comp4 = CreateCompilation("", references: new[] { comp1Emit }, options: WithNonNullTypesTrue());
            comp4.VerifyDiagnostics();
            verifyCompilation(comp4);

            var comp2Emit = comp2.EmitToImageReference();
            var comp5 = CreateCompilation("", references: new[] { comp2Emit }, options: WithNonNullTypesFalse());
            comp5.VerifyDiagnostics();
            verifyCompilation(comp5);

            var comp6 = CreateCompilation("", references: new[] { comp1Emit }, parseOptions: TestOptions.Regular7_3);
            comp6.VerifyDiagnostics();
            verifyCompilation(comp6);

            void verifyCompilation(CSharpCompilation compilation)
            {
                var c = ((Compilation)compilation).GetTypeByMetadataName("C");
                var members = c.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).ToArray();
                assertNullability(members,
                    PublicNullableAnnotation.None,
                    PublicNullableAnnotation.NotAnnotated,
                    PublicNullableAnnotation.None,
                    PublicNullableAnnotation.NotAnnotated);

                var e = ((Compilation)compilation).GetTypeByMetadataName("Ext");
                members = e.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("M")).Select(m => m.ReduceExtensionMethod(m.Parameters[0].Type)).ToArray();
                assertNullability(members,
                    PublicNullableAnnotation.NotAnnotated,
                    PublicNullableAnnotation.Annotated,
                    PublicNullableAnnotation.NotAnnotated,
                    PublicNullableAnnotation.Annotated,
                    PublicNullableAnnotation.None,
                    PublicNullableAnnotation.Annotated,
                    PublicNullableAnnotation.NotAnnotated,
                    PublicNullableAnnotation.Annotated);

                static void assertNullability(IMethodSymbol[] methods, params PublicNullableAnnotation[] expectedAnnotations)
                {
                    var actualAnnotations = methods.Select(m =>
                    {
                        var result = m.ReceiverType.NullableAnnotation;
                        Assert.Equal(result, m.ReceiverNullableAnnotation);
                        return result;
                    });
                    AssertEx.Equal(expectedAnnotations, actualAnnotations);
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
                    // (8,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(8, 2),
                    // (10,10): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? LM2(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(10, 10),
                    // (10,17): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? LM2(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(10, 17),
                    // (13,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(13, 2),
                    // (15,10): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(15, 10),
                    // (15,17): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //         C? LM6(C? c) => null;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(15, 17)
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
                    Assert.Equal(method.ReturnNullableAnnotation, method.Parameters[0].Type.NullableAnnotation);
                    Assert.Equal(PublicNullableAnnotation.None, method.ReceiverNullableAnnotation);
                    Assert.Equal(PublicNullableAnnotation.None, method.ReceiverType.NullableAnnotation);
                    var result = method.ReturnType.NullableAnnotation;
                    Assert.Equal(result, method.ReturnNullableAnnotation);
                    return result;
                },
                testMetadata: false,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
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
                    // (8,20): warning CS8618: Non-nullable event 'D1' is uninitialized. Consider declaring the event as nullable.
                    //     public event D D1;
                    Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "D1").WithArguments("event", "D1").WithLocation(8, 20),
                    // (13,19): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                    //     public event D? D4;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 19)
                },
                new[] { 
                    // (7,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(7, 2),
                    // (9,19): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public event D? D2;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(9, 19),
                    // (11,2): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(11, 2),
                    // (13,19): error CS8652: The feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     public event D? D4;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(13, 19)
                },
                compilation =>
                {
                    var c = ((Compilation)compilation).GetTypeByMetadataName("C");
                    return c.GetMembers().OfType<IEventSymbol>().ToArray();
                },
                member =>
                {
                    var result = member.Type.NullableAnnotation;
                    Assert.Equal(result, member.NullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void ArrayElements()
        {
            var source =
@"public interface I
{
#nullable enable
    object[] F1();
    object?[] F2();
    int[] F3();
    int?[] F4();
#nullable disable
    object[] F5();
    object?[] F6();
    int[] F7();
    int?[] F8();
}";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (10,11): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                    //     object?[] F6();
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(10, 11)
                },
                new[]
                {
                    // (3,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(3, 2),
                    // (5,11): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     object?[] F2();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(5, 11),
                    // (8,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(8, 2),
                    // (10,11): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     object?[] F6();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(10, 11)
                },
                comp => ((INamedTypeSymbol)((Compilation)comp).GetMember("I")).GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("F")).ToArray(),
                method =>
                {
                    var array = (IArrayTypeSymbol)method.ReturnType;
                    var result = array.ElementType.NullableAnnotation;
                    Assert.Equal(result, array.ElementNullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void TypeParameters()
        {
            var source =
@"#nullable enable
public interface I<T, U, V>
    where U : class
    where V : struct
{
    T F1();
    U F2();
    U? F3();
    V F4();
    V? F5();
#nullable disable
    T F6();
    U F7();
    U? F8();
    V F9();
    V? F10();
}";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (14,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                    //     U? F8();
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 6)
                },
                new[]
                {
                    // (1,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(1, 2),
                    // (8,6): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     U? F3();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(8, 6),
                    // (11,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(11, 2),
                    // (14,6): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     U? F8();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(14, 6)
                },
                comp => ((INamedTypeSymbol)((Compilation)comp).GetMember("I")).GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("F")).ToArray(),
                method =>
                {
                    var result = method.ReturnType.NullableAnnotation;
                    Assert.Equal(result, method.ReturnNullableAnnotation);
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void Constraints()
        {
            var source =
@"public class A<T>
{
    public class B<U> where U : T { }
}
public interface I
{
#nullable enable
    A<string> F1();
    A<string?> F2();
    A<int> F3();
    A<int?> F4();
#nullable disable
    A<string> F5();
    A<string?> F6();
    A<int> F7();
    A<int?> F8();
}";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (14,13): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                    //     A<object?> F6();
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(14, 13)
                },
                new[]
                {
                    // (7,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(7, 2),
                    // (9,13): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     A<string?> F2();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(9, 13),
                    // (12,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(12, 2),
                    // (14,13): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     A<string?> F6();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(14, 13)
                },
                comp => ((INamedTypeSymbol)((Compilation)comp).GetMember("I")).GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("F")).ToArray(),
                method =>
                {
                    ITypeParameterSymbol typeParameterSymbol = ((INamedTypeSymbol)((INamedTypeSymbol)method.ReturnType).GetMembers("B").Single()).TypeParameters.Single();
                    var result = typeParameterSymbol.ConstraintTypes.Single().NullableAnnotation;
                    Assert.Equal(result, typeParameterSymbol.ConstraintNullableAnnotations.Single());
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void TypeArguments_01()
        {
            var source =
@"public interface IA<T>
{
}
#nullable enable
public interface IB<T, U, V>
    where U : class
    where V : struct
{
    IA<T> F1();
    IA<U> F2();
    IA<U?> F3();
    IA<V> F4();
    IA<V?> F5();
#nullable disable
    IA<T> F6();
    IA<U> F7();
    IA<U?> F8();
    IA<V> F9();
    IA<V?> F10();
}";
            VerifyAcrossCompilations(
                source,
                new[]
                {
                    // (17,9): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                    //     IA<U?> F8();
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(17, 9)
                },
                new[]
                {
                    // (4,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable enable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(4, 2),
                    // (11,9): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     IA<U?> F3();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(11, 9),
                    // (14,2): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    // #nullable disable
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "nullable").WithArguments("nullable reference types", "8.0").WithLocation(14, 2),
                    // (17,9): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                    //     IA<U?> F8();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "?").WithArguments("nullable reference types", "8.0").WithLocation(17, 9)
                },
                comp => ((INamedTypeSymbol)((Compilation)comp).GetMember("IB")).GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("F")).ToArray(),
                method =>
                {
                    var result = ((INamedTypeSymbol)method.ReturnType).TypeArguments.Single().NullableAnnotation;
                    Assert.Equal(result, ((INamedTypeSymbol)method.ReturnType).TypeArgumentNullableAnnotations.Single());
                    Assert.Equal(result, ((INamedTypeSymbol)method.ReturnType).TypeArgumentNullableAnnotations().Single());
                    return result;
                },
                testMetadata: true,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void TypeArguments_02()
        {
            var source =
@"class C
{
    static void F<T>()
    {
    }
#nullable enable
    static void M<T, U, V>()
        where U : class
        where V : struct
    {
        F<T>();
        F<U>();
        F<U?>();
        F<V>();
        F<V?>();
#nullable disable
        F<T>();
        F<U>();
        F<U?>();
        F<V>();
        F<V?>();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (19,12): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //         F<U?>();
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(19, 12));
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var invocations = syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();
            var actualAnnotations = invocations.Select(inv =>
            {
                var method = (IMethodSymbol)model.GetSymbolInfo(inv).Symbol;
                var result = method.TypeArguments.Single().NullableAnnotation;
                Assert.Equal(result, method.TypeArgumentNullableAnnotations.Single());
                return result;
            }).ToArray();
            var expectedAnnotations = new[]
            {
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated
            };
            AssertEx.Equal(expectedAnnotations, actualAnnotations);
        }

        [Fact]
        [WorkItem(34412, "https://github.com/dotnet/roslyn/issues/34412")]
        public void Locals()
        {
            var source =
@"#pragma warning disable 168
class C
{
#nullable enable
    static void M<T, U, V>()
        where U : class
        where V : struct
    {
        T x1;
        U x2;
        U? x3;
        V x4;
        V? x5;
#nullable disable
        T x6;
        U x7;
        U? x8;
        V x9;
        V? x10;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,10): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //         U? x8;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(17, 10));
            var syntaxTree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(syntaxTree);
            var variables = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            var actualAnnotations = variables.Select(v =>
            {
                var localSymbol = (ILocalSymbol)model.GetDeclaredSymbol(v);
                var result = localSymbol.Type.NullableAnnotation;
                Assert.Equal(result, localSymbol.NullableAnnotation);
                return result;
            }).ToArray();

            var expectedAnnotations = new[]
            {
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.None,
                PublicNullableAnnotation.Annotated,
                PublicNullableAnnotation.NotAnnotated,
                PublicNullableAnnotation.Annotated
            };
            AssertEx.Equal(expectedAnnotations, actualAnnotations);
        }

        private static void VerifyAcrossCompilations<T>(string source,
                                                 DiagnosticDescription[] nullableEnabledErrors,
                                                 DiagnosticDescription[] nullableDisabledErrors,
                                                 Func<CSharpCompilation, T[]> memberFunc,
                                                 Func<T, PublicNullableAnnotation> nullabilityFunc,
                                                 bool testMetadata,
                                                 params PublicNullableAnnotation[] expectedNullabilities)
        {
            var comp1 = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp1.VerifyDiagnostics(nullableEnabledErrors);
            verifyCompilation(comp1);

            var comp2 = CreateCompilation(source, options: WithNonNullTypesFalse());
            comp2.VerifyDiagnostics(nullableEnabledErrors);
            verifyCompilation(comp2);

            var comp3 = CreateCompilation(source, parseOptions: TestOptions.Regular7_3, skipUsesIsNullable: true);
            comp3.VerifyDiagnostics(nullableDisabledErrors);
            verifyCompilation(comp3);

            if (!testMetadata) return;

            var comp1Emit = comp1.EmitToImageReference();
            var comp4 = CreateCompilation("", references: new[] { comp1Emit }, options: WithNonNullTypesTrue());
            comp4.VerifyDiagnostics();
            verifyCompilation(comp4);

            var comp2Emit = comp2.EmitToImageReference();
            var comp5 = CreateCompilation("", references: new[] { comp2Emit }, options: WithNonNullTypesFalse());
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());

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
        var o1 = o;
        if (o == null) return;
        var o2 = o;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());

            comp.VerifyDiagnostics();
            comp.VerifyAnalyzerDiagnostics(new[] { new NullabilityPrinter() }, null, null, true,
                Diagnostic("CA9998_NullabilityPrinter", "o = null").WithArguments("o", "Annotated").WithLocation(6, 17),
                Diagnostic("CA9998_NullabilityPrinter", "o1 = o").WithArguments("o1", "Annotated").WithLocation(7, 13),
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "MaybeNull", "Annotated", "MaybeNull").WithLocation(7, 18),
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "MaybeNull", "Annotated", "MaybeNull").WithLocation(8, 13),
                Diagnostic("CA9998_NullabilityPrinter", "o2 = o").WithArguments("o2", "NotAnnotated").WithLocation(9, 13),
                Diagnostic("CA9999_NullabilityPrinter", "o").WithArguments("o", "NotNull", "NotAnnotated", "NotNull").WithLocation(9, 18));
        }

        private class NullabilityPrinter : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_descriptor1, s_descriptor2);

            private static DiagnosticDescriptor s_descriptor1 = new DiagnosticDescriptor(id: "CA9999_NullabilityPrinter", title: "CA9999_NullabilityPrinter", messageFormat: "Nullability of '{0}' is '{1}':'{2}'. Speculative flow state is '{3}'", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);
            private static DiagnosticDescriptor s_descriptor2 = new DiagnosticDescriptor(id: "CA9998_NullabilityPrinter", title: "CA9998_NullabilityPrinter", messageFormat: "Declared nullability of '{0}' is '{1}'", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override void Initialize(AnalysisContext context)
            {
                var newSource = (ExpressionStatementSyntax)SyntaxFactory.ParseStatement("_ = o;");
                var oReference = ((AssignmentExpressionSyntax)newSource.Expression).Right;

                context.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    if (syntaxContext.Node.ToString() != "o") return;
                    var info = syntaxContext.SemanticModel.GetTypeInfo(syntaxContext.Node);
                    Assert.True(syntaxContext.SemanticModel.TryGetSpeculativeSemanticModel(syntaxContext.Node.SpanStart, newSource, out var specModel));
                    var specInfo = specModel.GetTypeInfo(oReference);
                    syntaxContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_descriptor1, syntaxContext.Node.GetLocation(), syntaxContext.Node, info.Nullability.FlowState, info.Nullability.Annotation, specInfo.Nullability.FlowState));
                }, SyntaxKind.IdentifierName);

                context.RegisterSyntaxNodeAction(context =>
                {
                    var declarator = (VariableDeclaratorSyntax)context.Node;
                    var declaredSymbol = (ILocalSymbol)context.SemanticModel.GetDeclaredSymbol(declarator);
                    Assert.Equal(declaredSymbol.Type.NullableAnnotation, declaredSymbol.NullableAnnotation);
                    context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_descriptor2, declarator.GetLocation(), declaredSymbol.Name, declaredSymbol.NullableAnnotation));

                }, SyntaxKind.VariableDeclarator);
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

            var comp = (Compilation)CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (10,17): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         var d = (D)(C?)new B();
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "(D)(C?)new B()").WithLocation(10, 17));

            var syntaxTree = comp.SyntaxTrees.First();
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var ifStatement = root.DescendantNodes().OfType<IfStatementSyntax>().Single();
            var conditionalAccessExpression = root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single();
            var ternary = root.DescendantNodes().OfType<ConditionalExpressionSyntax>().Single();

            var newSource = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ string? s3 = null; _ = s1 == """" ? s1 : s1; }");
            var newExprStatement = (ExpressionStatementSyntax)newSource.Statements[1];
            var newTernary = (ConditionalExpressionSyntax)((AssignmentExpressionSyntax)newExprStatement.Expression).Right;
            var inCondition = ((BinaryExpressionSyntax)newTernary.Condition).Left;
            var whenTrue = newTernary.WhenTrue;
            var whenFalse = newTernary.WhenFalse;

            var newReference = (IdentifierNameSyntax)SyntaxFactory.ParseExpression(@"s1");
            var newCoalesce = (AssignmentExpressionSyntax)SyntaxFactory.ParseExpression(@"s3 ??= s1", options: TestOptions.Regular8);

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

                var referenceTypeInfo = speculativeModel.GetSpeculativeTypeInfo(whenTrue.SpanStart, newReference, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(PublicNullableFlowState.NotNull, referenceTypeInfo.Nullability.FlowState);
                var coalesceTypeInfo = speculativeModel.GetSpeculativeTypeInfo(whenTrue.SpanStart, newCoalesce, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(PublicNullableFlowState.NotNull, coalesceTypeInfo.Nullability.FlowState);

                speculativeTypeInfo = speculativeModel.GetTypeInfo(whenFalse);
                Assert.Equal(conditionFlowState, speculativeTypeInfo.Nullability.FlowState);
                referenceTypeInfo = speculativeModel.GetSpeculativeTypeInfo(whenFalse.SpanStart, newReference, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(conditionFlowState, referenceTypeInfo.Nullability.FlowState);

                coalesceTypeInfo = speculativeModel.GetSpeculativeTypeInfo(whenFalse.SpanStart, newCoalesce, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(conditionFlowState, coalesceTypeInfo.Nullability.FlowState);
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

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
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

        [Fact]
        public void SpeculativeGetTypeInfo_Basic()
        {
            var source = @"
class C
{
    static object? staticField = null;
    object field = staticField is null ? new object() : staticField;

    string M(string? s1)
    {
        if (s1 != null)
        {
            s1.ToString();
        }

        s1?.ToString();

        s1 = """";
        var s2 = s1 == null ? """" : s1;

        return null!;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var ifStatement = root.DescendantNodes().OfType<IfStatementSyntax>().Single();
            var conditionalAccessExpression = root.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single();
            var ternary = root.DescendantNodes().OfType<ConditionalExpressionSyntax>().ElementAt(1);

            var newReference = (IdentifierNameSyntax)SyntaxFactory.ParseExpression(@"s1");
            var newCoalesce = (AssignmentExpressionSyntax)SyntaxFactory.ParseExpression(@"s1 ??= """"");

            verifySpeculativeTypeInfo(ifStatement.SpanStart, PublicNullableFlowState.MaybeNull);
            verifySpeculativeTypeInfo(ifStatement.Statement.SpanStart, PublicNullableFlowState.NotNull);

            verifySpeculativeTypeInfo(conditionalAccessExpression.SpanStart, PublicNullableFlowState.MaybeNull);
            verifySpeculativeTypeInfo(conditionalAccessExpression.WhenNotNull.SpanStart, PublicNullableFlowState.NotNull);

            verifySpeculativeTypeInfo(ternary.WhenTrue.SpanStart, PublicNullableFlowState.MaybeNull);
            verifySpeculativeTypeInfo(ternary.WhenFalse.SpanStart, PublicNullableFlowState.NotNull);

            void verifySpeculativeTypeInfo(int position, PublicNullableFlowState expectedFlowState)
            {
                var specTypeInfo = model.GetSpeculativeTypeInfo(position, newReference, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(expectedFlowState, specTypeInfo.Nullability.FlowState);
                specTypeInfo = model.GetSpeculativeTypeInfo(position, newCoalesce, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(PublicNullableFlowState.NotNull, specTypeInfo.Nullability.FlowState);
            }
        }

        [Fact]
        public void FeatureFlagTurnsOffNullableAnalysis()
        {
            var source =
@"
#nullable enable
class C
{
    void M()
    {
        object o = null;
    }
}
";

            var featureFlagOff = TestOptions.Regular8.WithFeature("run-nullable-analysis", "false");

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue(), parseOptions: featureFlagOff);
            comp.VerifyDiagnostics(
                    // (7,16): warning CS0219: The variable 'o' is assigned but its value is never used
                    //         object o = null;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "o").WithArguments("o").WithLocation(7, 16),
                    // (7,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
                    //         object o = null;
                    Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 20));

            Assert.False(comp.NullableSemanticAnalysisEnabled);
        }

        [Fact]
        public void SymbolInfo_Invocation_InferredArguments()
        {
            var source = @"
class C
{
    T Identity<T>(T t) => t;

    void M(string? s)
    {
        _ = Identity(s);
        if (s is null) return;
        _ = Identity(s);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            var symbolInfo = model.GetSymbolInfo(invocations[0]);
            verifySymbolInfo((IMethodSymbol)symbolInfo.Symbol, PublicNullableAnnotation.Annotated);
            symbolInfo = model.GetSymbolInfo(invocations[1]);
            verifySymbolInfo((IMethodSymbol)symbolInfo.Symbol, PublicNullableAnnotation.NotAnnotated);

            static void verifySymbolInfo(IMethodSymbol methodSymbol, PublicNullableAnnotation expectedAnnotation)
            {
                Assert.Equal(expectedAnnotation, methodSymbol.TypeArgumentNullableAnnotations.Single());
                Assert.Equal(expectedAnnotation, methodSymbol.TypeArguments.Single().NullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.Parameters.Single().NullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.Parameters.Single().Type.NullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.ReturnNullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.ReturnType.NullableAnnotation);
            }
        }

        [Fact]
        public void SymbolInfo_Invocation_LocalFunction()
        {
            var source = @"
using System.Collections.Generic;
class C
{

    void M(string? s)
    {
        _ = CreateList(s);
        if (s is null) return;
        _ = CreateList(s);

        List<T> CreateList<T>(T t) => null!;
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            var symbolInfo = model.GetSymbolInfo(invocations[0]);
            verifySymbolInfo((IMethodSymbol)symbolInfo.Symbol, PublicNullableAnnotation.Annotated);
            symbolInfo = model.GetSymbolInfo(invocations[1]);
            verifySymbolInfo((IMethodSymbol)symbolInfo.Symbol, PublicNullableAnnotation.NotAnnotated);

            static void verifySymbolInfo(IMethodSymbol methodSymbol, PublicNullableAnnotation expectedAnnotation)
            {
                Assert.Equal(expectedAnnotation, methodSymbol.TypeArgumentNullableAnnotations.Single());
                Assert.Equal(expectedAnnotation, methodSymbol.TypeArguments.Single().NullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.Parameters.Single().NullableAnnotation);
                Assert.Equal(expectedAnnotation, methodSymbol.Parameters.Single().Type.NullableAnnotation);
                Assert.Equal(expectedAnnotation, ((INamedTypeSymbol)methodSymbol.ReturnType).TypeArgumentNullableAnnotations.Single());
                Assert.Equal(expectedAnnotation, ((INamedTypeSymbol)methodSymbol.ReturnType).TypeArgumentNullableAnnotations().Single());
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Locals_Inference()
        {
            var source = @"
class C
{
    void M(string? s1, string s2)
    {
        var o1 = s1;
        var o2 = s2;
        if (s1 == null) return;
        var o3 = s1;
        s2 = null;
        var o4 = s2;        
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (10,14): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s2 = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(10, 14));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.Annotated);

            void assertAnnotation(VariableDeclaratorSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Locals_NoInference()
        {
            // All declarations are the opposite of inference
            var source = @"
#pragma warning disable CS8600
class C
{
    void M(string? s1, string s2)
    {
        string o1 = s1;
        string? o2 = s2;
        if (s1 == null) return;
        string? o3 = s1;
        s2 = null;
        string o4 = s2;        
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(VariableDeclaratorSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_SingleVariableDeclaration_Inference()
        {
            var source1 = @"
#pragma warning disable CS8600
class C
{
    void M(string? s1, string s2)
    {
        var (o1, o2) = (s1, s2);
        var (o3, o4) = (s2, s1);
        if (s1 == null) return;
        var (o5, o6) = (s1, s2);
        s2 = null;
        var (o7, o8) = (s1, s2);
    }
}";

            verifyCompilation(source1);

            var source2 = @"
#pragma warning disable CS8600
class C
{
    void M(string? s1, string s2)
    {
        (var o1, var o2) = (s1, s2);
        (var o3, var o4) = (s2, s1);
        if (s1 == null) return;
        (var o5, var o6) = (s1, s2);
        s2 = null;
        (var o7, var o8) = (s1, s2);
    }
}";

            verifyCompilation(source2);

            void verifyCompilation(string source)
            {
                var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
                comp.VerifyDiagnostics();

                var syntaxTree = comp.SyntaxTrees[0];
                var root = syntaxTree.GetRoot();
                var model = comp.GetSemanticModel(syntaxTree);

                var declarations = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();

                assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated, PublicNullableAnnotation.NotAnnotated);
                assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.Annotated);
                assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.NotAnnotated);
                assertAnnotation(declarations[4], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.Annotated);

                void assertAnnotation(AssignmentExpressionSyntax variable, PublicNullableAnnotation expectedAnnotation1, PublicNullableAnnotation expectedAnnotation2)
                {
                    var symbols = variable.DescendantNodes().OfType<SingleVariableDesignationSyntax>().Select(s => model.GetDeclaredSymbol(s)).Cast<ILocalSymbol>().ToList();
                    Assert.Equal(expectedAnnotation1, symbols[0].NullableAnnotation);
                    Assert.Equal(expectedAnnotation1, symbols[0].Type.NullableAnnotation);
                    Assert.Equal(expectedAnnotation2, symbols[1].NullableAnnotation);
                    Assert.Equal(expectedAnnotation2, symbols[1].Type.NullableAnnotation);
                }
            }
        }

        [Fact]
        public void GetDeclaredSymbol_SingleVariableDeclaration_MixedInference()
        {
            var source = @"
#pragma warning disable CS8600
class C
{
    void M(string? s1, string s2)
    {
        (string o1, var o2) = (s1, s2);
        (string? o3, var o4) = (s2, s1);
        if (s1 == null) return;
        (var o5, string? o6) = (s1, s2);
        s2 = null;
        (var o7, string o8) = (s1, s2);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated, PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[4], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(AssignmentExpressionSyntax variable, PublicNullableAnnotation expectedAnnotation1, PublicNullableAnnotation expectedAnnotation2)
            {
                var symbols = variable.DescendantNodes().OfType<SingleVariableDesignationSyntax>().Select(s => model.GetDeclaredSymbol(s)).Cast<ILocalSymbol>().ToList();
                Assert.Equal(expectedAnnotation1, symbols[0].NullableAnnotation);
                Assert.Equal(expectedAnnotation1, symbols[0].Type.NullableAnnotation);
                Assert.Equal(expectedAnnotation2, symbols[1].NullableAnnotation);
                Assert.Equal(expectedAnnotation2, symbols[1].Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_SpeculativeModel()
        {
            // All declarations are the opposite of inference
            var source = @"
#pragma warning disable CS8600
class C
{
    void M(string? s1, string s2)
    {
        string o1 = s1;
        string? o2 = s2;
        if (s1 == null) return;
        string? o3 = s1;
        s2 = null;
        string o4 = s2;        
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var s2Assignment = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();
            var lastDeclaration = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(3);
            var newDeclaration = SyntaxFactory.ParseStatement("var o5 = s2;");
            var newDeclarator = newDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

            Assert.True(model.TryGetSpeculativeSemanticModel(s2Assignment.SpanStart, newDeclaration, out var specModel));
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((ILocalSymbol)specModel.GetDeclaredSymbol(newDeclarator)).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((ILocalSymbol)specModel.GetDeclaredSymbol(newDeclarator)).Type.NullableAnnotation);

            Assert.True(model.TryGetSpeculativeSemanticModel(lastDeclaration.SpanStart, newDeclaration, out specModel));
            Assert.Equal(PublicNullableAnnotation.Annotated, ((ILocalSymbol)specModel.GetDeclaredSymbol(newDeclarator)).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((ILocalSymbol)specModel.GetDeclaredSymbol(newDeclarator)).Type.NullableAnnotation);
        }

        [Fact]
        public void GetDeclaredSymbol_Using()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C : IDisposable, IAsyncDisposable
{
    public void Dispose() => throw null!;
    public ValueTask DisposeAsync() => throw null!;
    async void M(C? c1)
    {
        using var c2 = c1;
        using var c3 = c1 ?? new C();
        using (var c4 = c1) {}
        using (var c5 = c1 ?? new C()) {}
        await using (var c6 = c1) {}
        await using (var c6 = c1 ?? new C()) {}
    }
}
";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[4], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[5], PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(VariableDeclaratorSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Fixed()
        {
            var source = @"
class C
{
    unsafe void M(object? o)
    {
        fixed (var o1 = o ?? new object())
        {
        }
    }
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue().WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,20): error CS0821: Implicitly-typed local variables cannot be fixed
                //         fixed (var o1 = o ?? new object())
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed, "o1 = o ?? new object()").WithLocation(6, 20));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declaration = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var symbol = (ILocalSymbol)model.GetDeclaredSymbol(declaration);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symbol.Type.NullableAnnotation);
        }

        [Fact]
        public void GetDeclaredSymbol_ForLoop()
        {
            var source = @"
class C
{
    void M(object? o1, object o2)
    {
        for (var o3 = o1; false; ) {}
        for (var o4 = o1 ?? o2; false; ) {}
        for (var o5 = o1, o6 = o2; false; ) {}
    }
}
";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (8,14): error CS0819: Implicitly-typed variables cannot have multiple declarators
                //         for (var o5 = o1, o6 = o2; false; ) {}
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var o5 = o1, o6 = o2").WithLocation(8, 14));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(VariableDeclaratorSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_OutVariable()
        {
            var source = @"
class C
{
    void Out(out object? o1, out object o2) => throw null!;
    void M()
    {
        Out(out var o1, out var o2);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToList();
            assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated);


            void assertAnnotation(SingleVariableDesignationSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_OutVariable_WithTypeInference()
        {
            var source = @"
#pragma warning disable CS8600
class C
{
    void Out<T>(T o1, out T o2) => throw null!;
    void M(object o1, object? o2)
    {
        Out(o1, out var o3);
        Out(o2, out var o4);
        o1 = null;
        Out(o1, out var o5);
        _ = o2 ?? throw null!;
        Out(o2, out var o6);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToList();
            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);


            void assertAnnotation(SingleVariableDesignationSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Switch()
        {
            var source = @"
class C
{
    void M(object? o)
    {
        switch (o)
        {
            case object o1:
                break;
            case var o2:
                break;
        }

        _ = o switch { {} o1 => o1, var o2 => o2 };
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.Annotated);

            void assertAnnotation(SingleVariableDesignationSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_InLambda()
        {
            var source = @"
using System;
class C
{
    void M(object? o)
    {
        Action a1 = () =>
        {
            var o1 = o;
        };

        if (o == null) return;

        Action a2 = () =>
        {
            var o1 = o;
        };
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();

            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(VariableDeclaratorSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Foreach_Inferred()
        {
            var source = @"
#pragma warning disable CS8600
using System.Collections.Generic;
class C
{
    List<T> GetList<T>(T t) => throw null!;
    void M(object o1, object? o2)
    {
        foreach (var o in GetList(o1)) {}
        foreach (var o in GetList(o2)) {}
        o1 = null;
        foreach (var o in GetList(o1)) {}
        _  = o2 ?? throw null!;
        foreach (var o in GetList(o2)) {}
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<ForEachStatementSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);

            void assertAnnotation(ForEachStatementSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Foreach_NoInference()
        {
            var source = @"
#pragma warning disable CS8600
using System.Collections.Generic;
class C
{
    List<T> GetList<T>(T t) => throw null!;
    void M(object o1, object? o2)
    {
        foreach (object? o in GetList(o1)) {}
        foreach (object o in GetList(o2)) {}
        o1 = null;
        foreach (object o in GetList(o1)) {}
        _  = o2 ?? throw null!;
        foreach (object? o in GetList(o2)) {}
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (10,25): warning CS8606: Possible null reference assignment to iteration variable
                //         foreach (object o in GetList(o2)) {}
                Diagnostic(ErrorCode.WRN_NullReferenceIterationVariable, "o").WithLocation(10, 25),
                // (12,25): warning CS8606: Possible null reference assignment to iteration variable
                //         foreach (object o in GetList(o1)) {}
                Diagnostic(ErrorCode.WRN_NullReferenceIterationVariable, "o").WithLocation(12, 25));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<ForEachStatementSyntax>().ToList();

            assertAnnotation(declarations[0], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[3], PublicNullableAnnotation.Annotated);

            void assertAnnotation(ForEachStatementSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [Fact]
        public void GetDeclaredSymbol_Foreach_Tuples_MixedInference()
        {
            var source = @"
#pragma warning disable CS8600
using System.Collections.Generic;
class C
{
    List<(T, T)> GetList<T>(T t) => throw null!;
    void M(object o1, object? o2)
    {
        foreach ((var o3, object? o4) in GetList(o1)) {}
        foreach ((var o3, object o4) in GetList(o2)) { o3.ToString(); }
        o1 = null;
        foreach ((var o3, object o4) in GetList(o1)) {}
        _  = o2 ?? throw null!;
        foreach ((var o3, object? o4) in GetList(o2)) {}
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var declarations = root.DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToList();

            // Some annotations are incorrect because of https://github.com/dotnet/roslyn/issues/37491

            assertAnnotation(declarations[0], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[1], PublicNullableAnnotation.Annotated);
            assertAnnotation(declarations[2], PublicNullableAnnotation.NotAnnotated); // Should be Annotated
            assertAnnotation(declarations[3], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[4], PublicNullableAnnotation.NotAnnotated); // Should be Annotated
            assertAnnotation(declarations[5], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[6], PublicNullableAnnotation.NotAnnotated);
            assertAnnotation(declarations[7], PublicNullableAnnotation.Annotated);

            void assertAnnotation(SingleVariableDesignationSyntax variable, PublicNullableAnnotation expectedAnnotation)
            {
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(variable);
                Assert.Equal(expectedAnnotation, symbol.NullableAnnotation);
                Assert.Equal(expectedAnnotation, symbol.Type.NullableAnnotation);
            }
        }

        [InlineData("true")]
        [InlineData("false")]
        [Theory, WorkItem(37659, "https://github.com/dotnet/roslyn/issues/37659")]
        public void InvalidCodeVar_GetsCorrectSymbol(string flagState)
        {
            var source = @"
public class C
{
    public void M(string s)
    {
        s. // no completion
        var o = new object;
    }
}
";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8.WithFeature("run-nullable-analysis", flagState));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var sRef = root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "s").Single();

            var info = model.GetSpeculativeSymbolInfo(sRef.Position, sRef, SpeculativeBindingOption.BindAsExpression);

            IParameterSymbol symbol = (IParameterSymbol)info.Symbol;
            Assert.True(info.CandidateSymbols.IsEmpty);
            Assert.NotNull(symbol);
            Assert.Equal("s", symbol.Name);
            Assert.Equal(SpecialType.System_String, symbol.Type.SpecialType);
        }

        [Fact, WorkItem(37879, "https://github.com/dotnet/roslyn/issues/37879")]
        public void MissingSymbols_ReinferredParent()
        {
            var source = @"
class C
{
    public void A<T>(T t) where T:class
    {
        var c = new F<T>[] { }.Select(v => new { Value = v.Item }).ToArray();
    }
    private class F<T>
    {
        public F(T oldItem) => Item = oldItem;
        public T Item { get; }
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var select = root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ValueText == "Select").Single();
            var symbolInfo = model.GetSymbolInfo(select);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Fact, WorkItem(37879, "https://github.com/dotnet/roslyn/issues/37879")]
        public void MultipleSymbols_ReinferredParent()
        {
            var source = @"
using System;
class C
{
    public void A<T>(T t) where T : class
    {
        var c = new F<T>[] { }.Select(v => new { Value = v.Item }).ToArray();
    }
    private class F<T>
    {
        public F(T oldItem) => Item = oldItem;
        public T Item { get; }
    }
}
static class ArrayExtensions
{
    public static U Select<T, U>(this T[] arr, Func<T, object, U> mapper, object arg) => throw null!;
    public static U Select<T, U, V>(this T[] arr, Func<T, V, U> mapper, V arg) => throw null!;
    public static U Select<T, U>(this T[] arr, C mapper) => throw null!;
    public static U Select<T, U>(this T[] arr, string mapper) => throw null!;
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var select = root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ValueText == "Select").Single();
            var symbolInfo = model.GetSymbolInfo(select);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(4, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void GetSymbolInfo_PropertySymbols()
        {
            var source = @"
class C<T>
{
    public T GetT { get; }

    static C<U> Create<U>(U u) => new C<U>();

    static void M(object? o)
    {
        var c1 = Create(o);
        _ = c1.GetT;
        if (o is null) return;
        var c2 = Create(o);
        _ = c2.GetT;
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (4,14): warning CS8618: Non-nullable property 'GetT' is uninitialized. Consider declaring the property as nullable.
                    //     public T GetT { get; }
                    Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "GetT").WithArguments("property", "GetT").WithLocation(4, 14));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

            var symInfo = model.GetSymbolInfo(memberAccess[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IPropertySymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IPropertySymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
            symInfo = model.GetSymbolInfo(memberAccess[1]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IPropertySymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IPropertySymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
        }

        [Fact]
        public void GetSymbolInfo_FieldSymbols()
        {
            var source = @"
class C<T>
{
    public T GetT;

    static C<U> Create<U>(U u) => new C<U>();

    static void M(object? o)
    {
        var c1 = Create(o);
        _ = c1.GetT;
        if (o is null) return;
        var c2 = Create(o);
        _ = c2.GetT;
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (4,14): warning CS8618: Non-nullable field 'GetT' is uninitialized. Consider declaring the field as nullable.
                    //     public T GetT;
                    Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "GetT").WithArguments("field", "GetT").WithLocation(4, 14),
                    // (4,14): warning CS0649: Field 'C<T>.GetT' is never assigned to, and will always have its default value 
                    //     public T GetT;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "GetT").WithArguments("C<T>.GetT", "").WithLocation(4, 14));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

            var symInfo = model.GetSymbolInfo(memberAccess[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IFieldSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IFieldSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
            symInfo = model.GetSymbolInfo(memberAccess[1]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IFieldSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IFieldSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
        }

        [Fact]
        public void GetSymbolInfo_EventAdditionSymbols()
        {
            var source = @"
#pragma warning disable CS0067
using System;
class C<T>
{
    public event EventHandler? Event;

    static C<U> Create<U>(U u) => new C<U>();

    static void M(object? o)
    {
        var c1 = Create(o);
        c1.Event += (obj, sender) => {};
        if (o is null) return;
        var c2 = Create(o);
        c2.Event += (obj, sender) => {};
        c2.Event += c1.Event;
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

            var symInfo = model.GetSymbolInfo(memberAccess[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
            symInfo = model.GetSymbolInfo(memberAccess[1]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());

            var event1 = model.GetSymbolInfo(memberAccess[2]).Symbol;
            var event2 = model.GetSymbolInfo(memberAccess[3]).Symbol;
            Assert.NotNull(event1);
            Assert.NotNull(event2);
            Assert.True(event1.Equals(event2, SymbolEqualityComparer.Default));
            Assert.False(event1.Equals(event2, SymbolEqualityComparer.IncludeNullability));
        }

        [Fact]
        public void GetSymbolInfo_EventAssignmentSymbols()
        {
            var source = @"
#pragma warning disable CS0067
using System;
class C<T>
{
    public event EventHandler? Event;

    static C<U> Create<U>(U u) => new C<U>();

    static void M(object? o)
    {
        var c1 = Create(o);
        c1.Event = (obj, sender) => {};
        if (o is null) return;
        var c2 = Create(o);
        c2.Event = (obj, sender) => {};
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

            var symInfo = model.GetSymbolInfo(memberAccess[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.Annotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
            symInfo = model.GetSymbolInfo(memberAccess[1]);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, ((IEventSymbol)symInfo.Symbol).Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations[0]);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, symInfo.Symbol.ContainingType.TypeArgumentNullableAnnotations().First());
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_InstanceMethods()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public void Add<T>(T t) => throw null!;
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
    }
}
";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().Single();

            verifyAnnotation(collectionInitializer.Expressions[0], PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[1], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[2], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[3], PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ExpressionSyntax expr, PublicNullableAnnotation expectedAnnotation)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArgumentNullableAnnotations[0]);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArguments[0].NullableAnnotation);
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_ExtensionMethod01()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
    }
}
static class CExt
{
    public static void Add<T>(this C c, T t) => throw null!;
}
";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().Single();

            verifyAnnotation(collectionInitializer.Expressions[0], PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[1], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[2], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[3], PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ExpressionSyntax expr, PublicNullableAnnotation expectedAnnotation)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArgumentNullableAnnotations[0]);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArguments[0].NullableAnnotation);
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_ExtensionMethod02()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
    }
}
static class CExt
{
    public static void Add<T, U>(this T t, U u) => throw null!;
}
";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().Single();

            verifyAnnotation(collectionInitializer.Expressions[0], PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[1], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[2], PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[3], PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ExpressionSyntax expr, PublicNullableAnnotation expectedAnnotation)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IMethodSymbol)symbolInfo.Symbol).TypeArgumentNullableAnnotations[0]);
                Assert.Equal(PublicNullableAnnotation.NotAnnotated, ((IMethodSymbol)symbolInfo.Symbol).TypeArguments[0].NullableAnnotation);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArgumentNullableAnnotations[1]);
                Assert.Equal(expectedAnnotation, ((IMethodSymbol)symbolInfo.Symbol).TypeArguments[1].NullableAnnotation);
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_MultipleOverloads()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
    }
}
static class CExt1
{
    public static void Add<T>(this C c, T t) => throw null!;
}
static class CExt2
{
    public static void Add<T>(this C c, T t) => throw null!;
}
";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (10,23): error CS0121: The call is ambiguous between the following methods or properties: 'CExt1.Add<T>(C, T)' and 'CExt2.Add<T>(C, T)'
                //         _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
                Diagnostic(ErrorCode.ERR_AmbigCall, "o1").WithArguments("CExt1.Add<T>(C, T)", "CExt2.Add<T>(C, T)").WithLocation(10, 23),
                // (10,27): error CS0121: The call is ambiguous between the following methods or properties: 'CExt1.Add<T>(C, T)' and 'CExt2.Add<T>(C, T)'
                //         _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
                Diagnostic(ErrorCode.ERR_AmbigCall, "Identity(o1 ??= new object())").WithArguments("CExt1.Add<T>(C, T)", "CExt2.Add<T>(C, T)").WithLocation(10, 27),
                // (10,58): error CS0121: The call is ambiguous between the following methods or properties: 'CExt1.Add<T>(C, T)' and 'CExt2.Add<T>(C, T)'
                //         _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
                Diagnostic(ErrorCode.ERR_AmbigCall, "o1").WithArguments("CExt1.Add<T>(C, T)", "CExt2.Add<T>(C, T)").WithLocation(10, 58),
                // (10,62): error CS0121: The call is ambiguous between the following methods or properties: 'CExt1.Add<T>(C, T)' and 'CExt2.Add<T>(C, T)'
                //         _ = new C() { o1, Identity(o1 ??= new object()), o1, o2 };
                Diagnostic(ErrorCode.ERR_AmbigCall, "o2").WithArguments("CExt1.Add<T>(C, T)", "CExt2.Add<T>(C, T)").WithLocation(10, 62));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().Single();

            verifyAnnotation(collectionInitializer.Expressions[0]);
            verifyAnnotation(collectionInitializer.Expressions[1]);
            verifyAnnotation(collectionInitializer.Expressions[2]);
            verifyAnnotation(collectionInitializer.Expressions[3]);

            void verifyAnnotation(ExpressionSyntax expr)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                Assert.Null(symbolInfo.Symbol);
                foreach (var symbol in symbolInfo.CandidateSymbols)
                {
                    Assert.Equal(PublicNullableAnnotation.None, ((IMethodSymbol)symbol).TypeArgumentNullableAnnotations[0]);
                    Assert.Equal(PublicNullableAnnotation.None, ((IMethodSymbol)symbol).TypeArguments[0].NullableAnnotation);
                }
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_MultiElementAdds()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { { o1, o2 }, { o2, o1 }, { Identity(o1 ??= new object()), o2 } };
    }
}
static class CExt
{
    public static void Add<T, U>(this C c, T t, U u) => throw null!;
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().First();

            verifyAnnotation(collectionInitializer.Expressions[0], PublicNullableAnnotation.Annotated, PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(collectionInitializer.Expressions[1], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[2], PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ExpressionSyntax expr, PublicNullableAnnotation annotation1, PublicNullableAnnotation annotation2)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                var methodSymbol = ((IMethodSymbol)symbolInfo.Symbol);
                Assert.Equal(annotation1, methodSymbol.TypeArgumentNullableAnnotations[0]);
                Assert.Equal(annotation1, methodSymbol.TypeArguments[0].NullableAnnotation);
                Assert.Equal(annotation2, methodSymbol.TypeArgumentNullableAnnotations[1]);
                Assert.Equal(annotation2, methodSymbol.TypeArguments[1].NullableAnnotation);
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredCollectionInitializerAdd_MultiElementAdds_LinkedTypes()
        {
            var source = @"
using System.Collections;
class C : IEnumerable
{
    public IEnumerator GetEnumerator() => throw null!;
    public static T Identity<T>(T t) => t;

    static void M(object? o1, string o2)
    {
        _ = new C() { { o1, o2 }, { o2, o1 }, { Identity(o1 ??= new object()), o2 } };
    }
}
static class CExt
{
    public static void Add<T>(this C c, T t1, T t2) => throw null!;
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var collectionInitializer = root.DescendantNodes().OfType<InitializerExpressionSyntax>().First();

            verifyAnnotation(collectionInitializer.Expressions[0], PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[1], PublicNullableAnnotation.Annotated);
            verifyAnnotation(collectionInitializer.Expressions[2], PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ExpressionSyntax expr, PublicNullableAnnotation annotation)
            {
                var symbolInfo = model.GetCollectionInitializerSymbolInfo(expr);
                var methodSymbol = ((IMethodSymbol)symbolInfo.Symbol);
                Assert.Equal(annotation, methodSymbol.TypeArgumentNullableAnnotations[0]);
                Assert.Equal(annotation, methodSymbol.TypeArguments[0].NullableAnnotation);
            }
        }

        [Fact]
        public void GetSymbolInfo_ReinferredIndexer()
        {
            var source = @"
class C<T, U>
{
    public T this[U u] { get => throw null!; set => throw null!; }
    
    public static void M(object? o1, object o2)
    {
        var c1 = CExt.Create(o1, o2);
        c1[o1] = o2;
        _ = c1[o1];
        
        var c2 = CExt.Create(o2, o1);
        c2[o2] = o1;
        _ = c2[o2];
        
        var c3 = CExt.Create(o1 ?? o2, o2);
        c3[o1] = o2;
        _ = c3[o1];
    }
}
static class CExt
{
    public static C<T, U> Create<T, U>(T t, U u) => throw null!;
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (9,12): warning CS8604: Possible null reference argument for parameter 'u' in 'object? C<object?, object>.this[object u]'.
                //         c1[o1] = o2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o1").WithArguments("u", "object? C<object?, object>.this[object u]").WithLocation(9, 12),
                // (10,16): warning CS8604: Possible null reference argument for parameter 'u' in 'object? C<object?, object>.this[object u]'.
                //         _ = c1[o1];
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o1").WithArguments("u", "object? C<object?, object>.this[object u]").WithLocation(10, 16),
                // (13,18): warning CS8601: Possible null reference assignment.
                //         c2[o2] = o1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "o1").WithLocation(13, 18),
                // (17,12): warning CS8604: Possible null reference argument for parameter 'u' in 'object C<object, object>.this[object u]'.
                //         c3[o1] = o2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o1").WithArguments("u", "object C<object, object>.this[object u]").WithLocation(17, 12),
                // (18,16): warning CS8604: Possible null reference argument for parameter 'u' in 'object C<object, object>.this[object u]'.
                //         _ = c3[o1];
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "o1").WithArguments("u", "object C<object, object>.this[object u]").WithLocation(18, 16));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var indexers = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToArray().AsSpan();
            verifyAnnotation(indexers.Slice(0, 2), PublicNullableAnnotation.Annotated, PublicNullableAnnotation.NotAnnotated);
            verifyAnnotation(indexers.Slice(2, 2), PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.Annotated);
            verifyAnnotation(indexers.Slice(4, 2), PublicNullableAnnotation.NotAnnotated, PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(Span<ElementAccessExpressionSyntax> indexers, PublicNullableAnnotation firstAnnotation, PublicNullableAnnotation secondAnnotation)
            {
                var propertySymbol = (IPropertySymbol)model.GetSymbolInfo(indexers[0]).Symbol;
                verifyIndexer(propertySymbol);
                propertySymbol = (IPropertySymbol)model.GetSymbolInfo(indexers[1]).Symbol;
                verifyIndexer(propertySymbol);

                void verifyIndexer(IPropertySymbol propertySymbol)
                {
                    Assert.True(propertySymbol.IsIndexer);
                    Assert.Equal(firstAnnotation, propertySymbol.NullableAnnotation);
                    Assert.Equal(firstAnnotation, propertySymbol.Type.NullableAnnotation);
                    Assert.Equal(secondAnnotation, propertySymbol.Parameters[0].NullableAnnotation);
                    Assert.Equal(secondAnnotation, propertySymbol.Parameters[0].Type.NullableAnnotation);
                }
            }
        }

        [Fact]
        public void GetSymbolInfo_IndexReinferred()
        {
            var source = @"
class C<T>
{
    public int Length { get; }
    public T this[int i] { get => throw null!; set => throw null!; }
    public static C<TT> Create<TT>(TT t) => throw null!;

    public static void M(object? o)
    {
        var c1 = Create(o);
        c1[^1] = new object();
        _ = c1[^1];

        var c2 = Create(o ?? new object());
        c2[^1] = new object();
        _ = c2[^1];
    }
}";

            var comp = CreateCompilationWithIndexAndRangeAndSpan(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var elementAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToArray().AsSpan();
            verifyAnnotation(elementAccesses.Slice(0, 2), PublicNullableAnnotation.Annotated);
            verifyAnnotation(elementAccesses.Slice(2, 2), PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(Span<ElementAccessExpressionSyntax> indexers, PublicNullableAnnotation annotation)
            {
                var propertySymbol = (IPropertySymbol)model.GetSymbolInfo(indexers[0]).Symbol;
                verifyIndexer(propertySymbol);
                propertySymbol = (IPropertySymbol)model.GetSymbolInfo(indexers[1]).Symbol;
                verifyIndexer(propertySymbol);

                void verifyIndexer(IPropertySymbol propertySymbol)
                {
                    Assert.True(propertySymbol.IsIndexer);
                    Assert.Equal(annotation, propertySymbol.NullableAnnotation);
                    Assert.Equal(annotation, propertySymbol.Type.NullableAnnotation);
                }
            }
        }

        [Fact]
        public void GetSymbolInfo_RangeReinferred()
        {
            var source = @"
using System;

class C<T>
{
    public int Length { get; }
    public Span<T> Slice(int start, int length) => throw null!;
    public static C<TT> Create<TT>(TT t) => throw null!;

    public static void M(object? o)
    {
        var c1 = Create(o);
        _ = c1[..^1];

        var c2 = Create(o ?? new object());
        _ = c2[..^1];
    }
}";

            var comp = CreateCompilationWithIndexAndRangeAndSpan(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var elementAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToArray();
            verifyAnnotation(elementAccesses[0], PublicNullableAnnotation.Annotated);
            verifyAnnotation(elementAccesses[1], PublicNullableAnnotation.NotAnnotated);

            void verifyAnnotation(ElementAccessExpressionSyntax indexer, PublicNullableAnnotation annotation)
            {
                var propertySymbol = (IMethodSymbol)model.GetSymbolInfo(indexer).Symbol;
                Assert.NotNull(propertySymbol);
                var spanType = (INamedTypeSymbol)propertySymbol.ReturnType;
                Assert.Equal(annotation, spanType.TypeArgumentNullableAnnotations[0]);
                Assert.Equal(annotation, spanType.TypeArgumentNullableAnnotations().First());
            }
        }

        [Fact]
        public void GetSymbolInfo_UnaryOperator()
        {
            var source =
@"#nullable enable
struct S<T>
{
    public static S<T> operator~(S<T> s) => s;
}
class Program
{
    static S<T> Create1<T>(T t) => new S<T>();
    static S<T>? Create2<T>(T t) => null;
    static void F<T>() where T : class, new()
    {
        T x = null;
        var sx = Create1(x);
        _ = ~sx;
        T? y = new T();
        var sy = Create2(y);
        _ = ~sy;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         T x = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(12, 15));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);
            var operators = root.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().ToList();
            verifyAnnotations(operators[0], PublicNullableAnnotation.Annotated, "S<T?> S<T?>.operator ~(S<T?> s)");
            verifyAnnotations(operators[1], PublicNullableAnnotation.NotAnnotated, "S<T!> S<T!>.operator ~(S<T!> s)");

            void verifyAnnotations(PrefixUnaryExpressionSyntax syntax, PublicNullableAnnotation annotation, string expected)
            {
                var method = (IMethodSymbol)model.GetSymbolInfo(syntax).Symbol;
                Assert.Equal(expected, method.ToTestDisplayString(includeNonNullable: true));
                Assert.Equal(annotation, method.ContainingType.TypeArgumentNullableAnnotations[0]);
                Assert.Equal(annotation, method.ContainingType.TypeArgumentNullableAnnotations().First());
            }
        }

        [Fact]
        public void GetSymbolInfo_BinaryOperator()
        {
            var source =
@"#nullable enable
struct S<T>
{
    public static S<T> operator+(S<T> x, S<T> y) => x;
}
class Program
{
    static S<T> Create1<T>(T t) => new S<T>();
    static S<T>? Create2<T>(T t) => null;
    static void F<T>() where T : class, new()
    {
        T x = null;
        var sx = Create1(x);
        _ = sx + sx;
        T? y = new T();
        var sy = Create2(y);
        _ = sy + sy;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         T x = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(12, 15));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);
            var operators = root.DescendantNodes().OfType<BinaryExpressionSyntax>().ToList();
            verifyAnnotations(operators[0], PublicNullableAnnotation.Annotated, "S<T?> S<T?>.operator +(S<T?> x, S<T?> y)");
            verifyAnnotations(operators[1], PublicNullableAnnotation.NotAnnotated, "S<T!> S<T!>.operator +(S<T!> x, S<T!> y)");

            void verifyAnnotations(BinaryExpressionSyntax syntax, PublicNullableAnnotation annotation, string expected)
            {
                var method = (IMethodSymbol)model.GetSymbolInfo(syntax).Symbol;
                Assert.Equal(expected, method.ToTestDisplayString(includeNonNullable: true));
                Assert.Equal(annotation, method.ContainingType.TypeArgumentNullableAnnotations[0]);
                Assert.Equal(annotation, method.ContainingType.TypeArgumentNullableAnnotations().First());
            }
        }

        [Fact]
        public void GetSymbolInfo_SimpleLambdaReinference()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => { _ = o1.ToString(); });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (9,39): warning CS8602: Dereference of a possibly null reference.
                    //         var a = Create(o, o1 => { _ = o1.ToString(); });
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o1").WithLocation(9, 39));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var lambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(lambda).Symbol;
            Assert.NotNull(lambdaSymbol);
            Assert.Equal(MethodKind.LambdaMethod, lambdaSymbol.MethodKind);
            Assert.Equal(PublicNullableAnnotation.Annotated, lambdaSymbol.Parameters[0].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, lambdaSymbol.Parameters[0].Type.NullableAnnotation);

            var o1Ref = lambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Single()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o1");

            var parameterSymbol = (IParameterSymbol)model.GetSymbolInfo(o1Ref).Symbol;
            Assert.NotNull(parameterSymbol);
            Assert.Equal(PublicNullableAnnotation.Annotated, parameterSymbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, parameterSymbol.Type.NullableAnnotation);

            var mDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.ValueText == "M");
            var mSymbol = model.GetDeclaredSymbol(mDeclaration);
            Assert.Equal(mSymbol, lambdaSymbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinference_NestedReinferred()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => {
            if (o1 == null) return;
            Create(o1, o2 => { _ = o2; _ = o1; });
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;

            var innerLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);

            var innerLambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(innerLambda).Symbol;
            Assert.NotNull(innerLambdaSymbol);
            Assert.Equal(MethodKind.LambdaMethod, innerLambdaSymbol.MethodKind);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol.Parameters[0].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol.Parameters[0].Type.NullableAnnotation);
            Assert.Equal(lambdaSymbol, innerLambdaSymbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            var o1Ref = innerLambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .ElementAt(1)
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o1");

            var o1Symbol = (IParameterSymbol)model.GetSymbolInfo(o1Ref).Symbol;
            Assert.Equal(PublicNullableAnnotation.Annotated, o1Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, o1Symbol.Type.NullableAnnotation);

            var o2Ref = innerLambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .First()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o2");

            var o2Symbol = (IParameterSymbol)model.GetSymbolInfo(o2Ref).Symbol;
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.Type.NullableAnnotation);
            Assert.Equal(innerLambdaSymbol, o2Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinference_NestedNotReinferred()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => {
            if (o1 == null) return;
            Action<string> a = o2 => { _ = o2; _ = o1; };
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;

            var innerLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);

            var innerLambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(innerLambda).Symbol;
            Assert.NotNull(innerLambdaSymbol);
            Assert.Equal(MethodKind.LambdaMethod, innerLambdaSymbol.MethodKind);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol.Parameters[0].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol.Parameters[0].Type.NullableAnnotation);
            Assert.Equal(lambdaSymbol, innerLambdaSymbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            var o1Ref = innerLambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .ElementAt(1)
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o1");

            var o1Symbol = (IParameterSymbol)model.GetSymbolInfo(o1Ref).Symbol;
            Assert.Equal(PublicNullableAnnotation.Annotated, o1Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.Annotated, o1Symbol.Type.NullableAnnotation);

            var o2Ref = innerLambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .First()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o2");

            var o2Symbol = (IParameterSymbol)model.GetSymbolInfo(o2Ref).Symbol;
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.Type.NullableAnnotation);
            Assert.Equal(innerLambdaSymbol, o2Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/38922")]
        public void NestedLambdaReinference_LocalFunctionInLambda()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => {
            LocalFunction(o1);
            void LocalFunction(object? o2) 
            {
                _ = o2;
            }
        });
    }
}";
            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var lambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(lambda).Symbol;

            var localFunction = lambda.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var localFunctionSymbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);

            var o2Reference = localFunction.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id => id.Identifier.ValueText == "o2");
            var o2Symbol = model.GetSymbolInfo(o2Reference).Symbol;

            Assert.Equal(lambdaSymbol, localFunctionSymbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(localFunctionSymbol, o2Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinferrence_SpeculativeParamReference()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => { _ = o1.ToString(); });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (9,39): warning CS8602: Dereference of a possibly null reference.
                    //         var a = Create(o, o1 => { _ = o1.ToString(); });
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o1").WithLocation(9, 39));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var o1Ref = lambda.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Single()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(i => i.Identifier.ValueText == "o1");
            var parameterSymbol = (IParameterSymbol)model.GetSymbolInfo(o1Ref).Symbol;

            var newStatement = (ExpressionStatementSyntax)SyntaxFactory.ParseStatement("_ = o1;");
            var newReference = ((AssignmentExpressionSyntax)newStatement.Expression).Right;

            Assert.True(model.TryGetSpeculativeSemanticModel(lambda.Body.SpanStart, newStatement, out var speculativeModel));
            var info = speculativeModel.GetSymbolInfo(newReference);

            Assert.Equal(parameterSymbol, info.Symbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinferrence_GetDeclaredSymbolParameter()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => { });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();
            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var lambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(lambda).Symbol;
            var parameter = lambda.DescendantNodes().OfType<ParameterSyntax>().Single();
            var paramSymbol = model.GetDeclaredSymbol(parameter);
            Assert.Equal(lambdaSymbol, paramSymbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinferrence_NestedLocalDeclaration()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => 
        {
            var o2 = o1 ?? new object();
            Action nested = () => { _ = o2; };

            foreach (var o3 in new int[] {}) {}
            foreach (var (o4, o5) in new (object, object)[]{}) {}
            (var o6, var o7) = (new object(), new object());

            void localFunc(out object? o)
            {
                o = null;
                var o8 = new object();
            }
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (18,18): warning CS8321: The local function 'localFunc' is declared but never used
                    //             void localFunc(out object? o)
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "localFunc").WithArguments("localFunc").WithLocation(18, 18));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;
            var o2Declaration = lambda.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var o2Symbol = model.GetDeclaredSymbol(o2Declaration);

            Assert.NotNull(lambdaSymbol);
            assertParent(o2Declaration);

            var innerLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);
            var innerO2Reference = innerLambda.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id => id.Identifier.ValueText == "o2");
            var o2Ref = model.GetSymbolInfo(innerO2Reference);

            Assert.Equal(o2Symbol, o2Ref.Symbol, SymbolEqualityComparer.IncludeNullability);

            var @foreach = lambda.DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            assertParent(@foreach);

            foreach (var singleVarDesignation in lambda.DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                assertParent(singleVarDesignation);
            }

            var localFunction = lambda.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var localFunctionSymbol = model.GetDeclaredSymbol(localFunction);

            var o8Declaration = localFunction.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            Assert.Equal(localFunctionSymbol, model.GetDeclaredSymbol(o8Declaration).ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            void assertParent(SyntaxNode node)
            {
                Assert.Equal(lambdaSymbol, model.GetDeclaredSymbol(node).ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            }
        }

        [Fact]
        public void NestedLambdaReinferrence_InInitializers()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;
    public static object? s_o = null;

    public Action<object> f = Create(s_o ?? new object(), o1 => { 
        var o2 = o1;
    });

    public Action<object> Prop { get; } = Create(s_o ?? new object(), o3 => { var o4 = o3; });
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();


            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var fieldLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var fieldLambdaSymbol = model.GetSymbolInfo(fieldLambda).Symbol;
            var o1Reference = fieldLambda.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id => id.Identifier.ValueText == "o1");
            var o1Symbol = (IParameterSymbol)model.GetSymbolInfo(o1Reference).Symbol;
            var o2Decl = fieldLambda.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var o2Symbol = (ILocalSymbol)model.GetDeclaredSymbol(o2Decl);

            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o1Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o1Symbol.Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o2Symbol.Type.NullableAnnotation);
            Assert.Equal(fieldLambdaSymbol, o1Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(fieldLambdaSymbol, o2Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            var propertyLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);
            var propertyLambdaSymbol = model.GetSymbolInfo(propertyLambda).Symbol;
            var o3Reference = propertyLambda.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id => id.Identifier.ValueText == "o3");
            var o3Symbol = (IParameterSymbol)model.GetSymbolInfo(o3Reference).Symbol;
            var o4Decl = propertyLambda.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var o4Symbol = (ILocalSymbol)model.GetDeclaredSymbol(o4Decl);

            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o3Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o3Symbol.Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o4Symbol.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, o4Symbol.Type.NullableAnnotation);
            Assert.Equal(propertyLambdaSymbol, o3Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(propertyLambdaSymbol, o4Symbol.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void NestedLambdaReinferrence_PartialExplicitTypes()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;
    public static Action<T> Create<T>(T t, Action<T, T, T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => {
            if (o1 == null) return;
            Create(o1, (o2, object o3, object? o4) => { });
            Create(o1, (object o2, object? o3, o4) => { });
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (12,29): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                    //             Create(o1, (o2, object o3, object? o4) => { });
                    Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "object").WithLocation(12, 29),
                    // (12,40): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                    //             Create(o1, (o2, object o3, object? o4) => { });
                    Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "object?").WithLocation(12, 40),
                    // (13,48): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                    //             Create(o1, (object o2, object? o3, o4) => { });
                    Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "o4").WithLocation(13, 48));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;

            var innerLambda1 = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);
            var innerLambdaSymbol1 = (IMethodSymbol)model.GetSymbolInfo(innerLambda1).Symbol;
            Assert.Equal(lambdaSymbol, innerLambdaSymbol1.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[0].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[0].Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[1].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[1].Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[2].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol1.Parameters[2].Type.NullableAnnotation);

            var innerLambda2 = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);
            var innerLambdaSymbol2 = (IMethodSymbol)model.GetSymbolInfo(innerLambda2).Symbol;
            Assert.Equal(lambdaSymbol, innerLambdaSymbol1.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[0].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[0].Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[1].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[1].Type.NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[2].NullableAnnotation);
            Assert.Equal(PublicNullableAnnotation.NotAnnotated, innerLambdaSymbol2.Parameters[2].Type.NullableAnnotation);
        }

        [Fact]
        public void NestedLambdaReinferrence_AttributeAndInitializers()
        {
            var source = @"
using System;
[AttributeUsage(AttributeTargets.All)]
class A : Attribute
{
    public A(object a) {}
}
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => 
        {
            var o2 = o1 ?? new object();

            void localFunc([A(o1)] object o3 = o2)
            {
                o = null;
                var o8 = new object();
            }
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (18,18): warning CS8321: The local function 'localFunc' is declared but never used
                    //             void localFunc([A(o1)] object o3 = o2)
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "localFunc").WithArguments("localFunc").WithLocation(18, 18),
                    // (18,28): error CS8205: Attributes are not allowed on local function parameters or type parameters
                    //             void localFunc([A(o1)] object o3 = o2)
                    Diagnostic(ErrorCode.ERR_AttributesInLocalFuncDecl, "[A(o1)]").WithLocation(18, 28),
                    // (18,31): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                    //             void localFunc([A(o1)] object o3 = o2)
                    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "o1").WithLocation(18, 31),
                    // (18,48): error CS1736: Default parameter value for 'o3' must be a compile-time constant
                    //             void localFunc([A(o1)] object o3 = o2)
                    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "o2").WithArguments("o3").WithLocation(18, 48));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();
            var o1Decl = lambda.Parameter;
            var o1Symbol = model.GetDeclaredSymbol(o1Decl);
            var o2Decl = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            var o2Symbol = model.GetDeclaredSymbol(o2Decl);

            var o1Ref = root.DescendantNodes().OfType<AttributeArgumentSyntax>().Last().Expression;
            var o1RefSymbol = model.GetSymbolInfo(o1Ref).Symbol;

            var o2Ref = root.DescendantNodes().OfType<ParameterSyntax>().Last().Default.Value;
            var o2RefSymbol = model.GetSymbolInfo(o2Ref).Symbol;

            Assert.Equal(o1Symbol, o1RefSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(o2Symbol, o2RefSymbol, SymbolEqualityComparer.IncludeNullability);

            var localFunction = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();

            var speculativeAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("A"), SyntaxFactory.ParseAttributeArgumentList("(o2)"));
            var speculativeO2Ref = speculativeAttribute.DescendantNodes().OfType<AttributeArgumentSyntax>().Single().Expression;
            Assert.True(model.TryGetSpeculativeSemanticModel(localFunction.SpanStart, speculativeAttribute, out var speculativeModel));
            Assert.Equal(o2Symbol, speculativeModel.GetSymbolInfo(speculativeO2Ref).Symbol, SymbolEqualityComparer.IncludeNullability);

            var speculativeInitializer = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("o1"));
            var speculativeO1Ref = speculativeInitializer.Value;
            Assert.True(model.TryGetSpeculativeSemanticModel(localFunction.ParameterList.Parameters[0].Default.SpanStart, speculativeInitializer, out speculativeModel));
            Assert.Equal(o1Symbol, speculativeModel.GetSymbolInfo(speculativeO1Ref).Symbol, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact]
        public void LookupSymbols_ReinferredSymbols()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => 
        {
            var o2 = o1 ?? new object();
            Action nested = () => { _ = o2; };

            foreach (var o3 in new int[] {}) {}
            foreach (var (o4, o5) in new (object, object)[]{}) {}
            (var o6, var o7) = (new object(), new object());

            void localFunc(out object? o)
            {
                o = null;
                var o8 = new object();
            }
        });
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                    // (18,18): warning CS8321: The local function 'localFunc' is declared but never used
                    //             void localFunc(out object? o)
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "localFunc").WithArguments("localFunc").WithLocation(18, 18));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);


            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;
            var innerLambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ElementAt(1);
            var localFunction = lambda.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var localFunctionSymbol = model.GetDeclaredSymbol(localFunction);

            var position = localFunction.DescendantNodes().OfType<VariableDeclarationSyntax>().Single().Span.End;

            var lookupResults = model.LookupSymbols(position);

            var o2Result = lookupResults.OfType<ILocalSymbol>().First(l => l.Name == "o2");
            var o8Result = lookupResults.OfType<ILocalSymbol>().First(l => l.Name == "o8");
            Assert.Equal(lambdaSymbol, o2Result.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(localFunctionSymbol, o8Result.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            var o1Result = lookupResults.OfType<IParameterSymbol>().First(p => p.Name == "o1");
            var oResult = lookupResults.OfType<IParameterSymbol>().First(p => p.Name == "o");
            Assert.Equal(lambdaSymbol, o1Result.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);
            Assert.Equal(localFunctionSymbol, oResult.ContainingSymbol, SymbolEqualityComparer.IncludeNullability);

            var localFunctionResult = lookupResults.OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.LocalFunction);
            Assert.Equal(localFunctionSymbol, localFunctionResult, SymbolEqualityComparer.IncludeNullability);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/38922")]
        public void LocalFunction_GenericTypeParameters()
        {
            var source = @"
using System;
class C
{
    public static Action<T> Create<T>(T t, Action<T> a) => throw null!;
	public static T[] Create<T>(T t) => throw null!;

    public static void M(object? o)
    {
        var a = Create(o, o1 => {
            LocalFunction(o1);
            T LocalFunction<T>(T t) 
            {
                _ = Create(t); // Type argument for Create needs to be reparented
				var d = new D<T>(); // Type argument in D's substituted type needs to be reparented
				d.DoSomething(t); // Argument of the function needs to be reparented
                var f = SecondFunction(); // Return type of nested function needs to be reparented
				return d.Prop; // Return type needs to be reparented
                T SecondFunction() { return t; }
            }
        });
    }
}
class D<T>
{
	public void DoSomething(T t) => throw null!;
	public T Prop { get; } = default!;
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambda = root.DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambdaSymbol = model.GetSymbolInfo(lambda).Symbol;
            var localFunction = lambda.DescendantNodes().OfType<LocalFunctionStatementSyntax>().First();
            var localFunctionSymbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);
            var nestedLocalFunction = (IMethodSymbol)model.GetDeclaredSymbol(lambda.DescendantNodes().OfType<LocalFunctionStatementSyntax>().ElementAt(1));

            var typeParameters = localFunctionSymbol.TypeParameters[0];
            Assert.Same(localFunctionSymbol, typeParameters.ContainingSymbol);
        }

        [Fact]
        public void SpeculativeModel_InAttribute()
        {
            var source = @"
using System;
[AttributeUsage(AttributeTargets.ReturnValue)]
class Attr : Attribute
{
    public Attr(string Test) {}
}
class Test
{
    const string Constant = ""Test"";
    [return: Attr(""Test"")]
    void M() {}
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var attributeUsage = root.DescendantNodes().OfType<AttributeSyntax>().ElementAt(1);
            var newAttributeUsage = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Attr"), SyntaxFactory.ParseAttributeArgumentList("(Constant)"));

            Assert.True(model.TryGetSpeculativeSemanticModel(attributeUsage.SpanStart, newAttributeUsage, out var specModel));
            Assert.NotNull(specModel);

            var symbolInfo = specModel.GetSymbolInfo(newAttributeUsage.ArgumentList.Arguments[0].Expression);
            Assert.Equal(SpecialType.System_String, ((IFieldSymbol)symbolInfo.Symbol).Type.SpecialType);
        }
    }
}
