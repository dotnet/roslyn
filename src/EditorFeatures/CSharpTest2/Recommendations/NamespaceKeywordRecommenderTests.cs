// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NamespaceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespaceKeyword()
            => VerifyAbsence(@"namespace $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"extern alias goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"extern alias goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsingAlias()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Goo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsingAlias_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Goo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedDelegateDeclaration()
        {
            VerifyAbsence(
@"class C {
    delegate void D();
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedMember()
        {
            VerifyAbsence(@"class A {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespaceKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    namespace $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    $$
    using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMember_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMember_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeExtern()
        {
            VerifyAbsence(@"$$
extern alias Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            VerifyAbsence(@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBetweenUsings()
        {
            VerifyAbsence(AddInsideMethod(
@"using Goo;
$$
using Bar;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalAttribute()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"[assembly: Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalAttribute_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"[assembly: Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAttribute()
        {
            VerifyAbsence(
@"[Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedAttribute()
        {
            VerifyAbsence(
@"class C {
  [Goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRegion()
        {
            VerifyKeyword(SourceCodeKind.Regular,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRegion_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }
    }
}
