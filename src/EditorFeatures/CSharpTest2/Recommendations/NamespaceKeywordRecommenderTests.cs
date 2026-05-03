// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class NamespaceKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"$$");

    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public async Task TestNotAfterNamespaceKeyword()
        => await VerifyAbsenceAsync(@"namespace $$");

    [Fact]
    public Task TestAfterPreviousNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestNotAfterPreviousFileScopedNamespace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            namespace N;
            $$
            """);

    [Fact]
    public Task TestNotAfterUsingInFileScopedNamespace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            namespace N;
            using U;
            $$
            """);

    [Fact]
    public Task TestAfterUsingInNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N
            {
                using U;
                $$
            }
            """);

    [Fact]
    public Task TestAfterPreviousNamespace_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            extern alias goo;
            $$
            """);

    [Fact]
    public Task TestAfterExtern_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            extern alias goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterExternInFileScopedNamespace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            namespace N;
            extern alias A;
            $$
            """);

    [Fact]
    public Task TestAfterExternInNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N
            {
                extern alias A;
                $$
            }
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsingAlias()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestAfterUsingAlias_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsingAlias()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            global using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsingAlias_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            global using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestAfterClassDeclaration()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterClassDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            delegate void D();
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            delegate void D();
            $$
            """);

    [Fact]
    public Task TestNotAfterNestedDelegateDeclaration()
        => VerifyAbsenceAsync(
            """
            class C {
                delegate void D();
                $$
            """);

    [Fact]
    public Task TestNotAfterNestedMember()
        => VerifyAbsenceAsync("""
            class A {
                class C {}
                $$
            """);

    [Fact]
    public Task TestInsideNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N {
                $$
            """);

    [Fact]
    public Task TestInsideNamespace_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            namespace N {
                $$
            """);

    [Fact]
    public Task TestNotAfterNamespaceKeyword_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                namespace $$
            """);

    [Fact]
    public Task TestAfterPreviousNamespace_InsideNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N {
               namespace N1 {}
               $$
            """);

    [Fact]
    public Task TestAfterPreviousNamespace_InsideNamespace_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            namespace N {
               namespace N1 {}
               $$
            """);

    [Fact]
    public Task TestNotBeforeUsing_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                $$
                using Goo;
            """);

    [Fact]
    public Task TestAfterMember_InsideNamespace()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            namespace N {
                class C {}
                $$
            """);

    [Fact]
    public Task TestAfterMember_InsideNamespace_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            namespace N {
                class C {}
                $$
            """);

    [Fact]
    public Task TestNotAfterNestedMember_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                class A {
                  class C {}
                  $$
            """);

    [Fact]
    public Task TestNotBeforeExtern()
        => VerifyAbsenceAsync("""
            $$
            extern alias Goo;
            """);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync("""
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync("""
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestNotBetweenUsings()
        => VerifyAbsenceAsync(
            """
            using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestNotBetweenGlobalUsings_01()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestNotBetweenGlobalUsings_02()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            $$
            global using Bar;
            """);

    [Fact]
    public Task TestAfterGlobalAttribute()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            [assembly: Goo]
            $$
            """);

    [Fact]
    public Task TestAfterGlobalAttribute_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            [assembly: Goo]
            $$
            """);

    [Fact]
    public Task TestNotAfterAttribute()
        => VerifyAbsenceAsync(
            """
            [Goo]
            $$
            """);

    [Fact]
    public Task TestNotAfterNestedAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
              [Goo]
              $$
            """);

    [Fact]
    public Task TestAfterRegion()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
    """
    #region EDM Relationship Metadata

    [assembly: EdmRelationshipAttribute("PerformanceResultsModel", "FK_Runs_Machines", "Machines", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), "Runs", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

    #endregion

    $$
    """);

    [Fact]
    public Task TestAfterRegion_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
    """
    #region EDM Relationship Metadata

    [assembly: EdmRelationshipAttribute("PerformanceResultsModel", "FK_Runs_Machines", "Machines", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), "Runs", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

    #endregion

    $$
    """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);
}
