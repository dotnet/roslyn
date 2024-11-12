// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NamespaceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"$$");
        }

        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterNamespaceKeyword()
            => await VerifyAbsenceAsync(@"namespace $$");

        [Fact]
        public async Task TestAfterPreviousNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPreviousFileScopedNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                namespace N;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsingInFileScopedNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                namespace N;
                using U;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingInNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N
                {
                    using U;
                    $$
                }
                """);
        }

        [Fact]
        public async Task TestAfterPreviousNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                extern alias goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExtern_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                extern alias goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterExternInFileScopedNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                namespace N;
                extern alias A;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExternInNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N
                {
                    extern alias A;
                    $$
                }
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingAlias()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingAlias_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsingAlias()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                global using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsingAlias_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                global using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterClassDeclaration()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterClassDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                delegate void D();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                delegate void D();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedDelegateDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    delegate void D();
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedMember()
        {
            await VerifyAbsenceAsync("""
                class A {
                    class C {}
                    $$
                """);
        }

        [Fact]
        public async Task TestInsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N {
                    $$
                """);
        }

        [Fact]
        public async Task TestInsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                namespace N {
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNamespaceKeyword_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    namespace $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousNamespace_InsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N {
                   namespace N1 {}
                   $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousNamespace_InsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                namespace N {
                   namespace N1 {}
                   $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    $$
                    using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterMember_InsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                namespace N {
                    class C {}
                    $$
                """);
        }

        [Fact]
        public async Task TestAfterMember_InsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                namespace N {
                    class C {}
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedMember_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    class A {
                      class C {}
                      $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeExtern()
        {
            await VerifyAbsenceAsync("""
                $$
                extern alias Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync("""
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync("""
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBetweenUsings()
        {
            await VerifyAbsenceAsync(
                """
                using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestNotBetweenGlobalUsings_01()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestNotBetweenGlobalUsings_02()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestAfterGlobalAttribute()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
                """
                [assembly: Goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalAttribute_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                [assembly: Goo]
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
                """
                [Goo]
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                  [Goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterRegion()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
        """
        #region EDM Relationship Metadata

        [assembly: EdmRelationshipAttribute("PerformanceResultsModel", "FK_Runs_Machines", "Machines", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), "Runs", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

        #endregion

        $$
        """);
        }

        [Fact]
        public async Task TestAfterRegion_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
        """
        #region EDM Relationship Metadata

        [assembly: EdmRelationshipAttribute("PerformanceResultsModel", "FK_Runs_Machines", "Machines", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), "Runs", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

        #endregion

        $$
        """);
        }
    }
}
