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
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNamespaceKeyword()
            => VerifyAbsence(@"namespace $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExtern()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"extern alias goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExtern_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"extern alias goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsing()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsingAlias()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Goo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsingAlias_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Goo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClassDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClassDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegateDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegateDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNestedDelegateDeclaration()
        {
            VerifyAbsence(
@"class C {
    delegate void D();
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNestedMember()
        {
            VerifyAbsence(@"class A {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNamespaceKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    namespace $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousNamespace_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousNamespace_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    $$
    using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMember_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMember_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNestedMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeExtern()
        {
            VerifyAbsence(@"$$
extern alias Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing()
        {
            VerifyAbsence(@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBetweenUsings()
        {
            VerifyAbsence(AddInsideMethod(
@"using Goo;
$$
using Bar;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalAttribute()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"[assembly: Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalAttribute_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"[assembly: Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterAttribute()
        {
            VerifyAbsence(
@"[Goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNestedAttribute()
        {
            VerifyAbsence(
@"class C {
  [Goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRegion()
        {
            VerifyKeyword(SourceCodeKind.Regular,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRegion_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }
    }
}
