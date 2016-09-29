// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NamespaceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespaceKeyword()
        {
            await VerifyAbsenceAsync(@"namespace $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"extern alias foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"extern alias foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsingAlias()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"using Foo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsingAlias_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"using Foo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassDeclaration()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"delegate void D();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedDelegateDeclaration()
        {
            await VerifyAbsenceAsync(
@"class C {
    delegate void D();
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedMember()
        {
            await VerifyAbsenceAsync(@"class A {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespaceKeyword_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    namespace $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_InsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNamespace_InsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    $$
    using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMember_InsideNamespace()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMember_InsideNamespace_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedMember_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeExtern()
        {
            await VerifyAbsenceAsync(@"$$
extern alias Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBetweenUsings()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using Foo;
$$
using Bar;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalAttribute()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"[assembly: Foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalAttribute_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"[assembly: Foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"[Foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
  [Foo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRegion()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRegion_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }
    }
}
