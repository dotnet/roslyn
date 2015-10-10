﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NamespaceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNamespaceKeyword()
        {
            VerifyAbsence(@"namespace $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"extern alias foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"extern alias foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsing()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsingAlias()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"using Foo = Bar;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsingAlias_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using Foo = Bar;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClassDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"class C {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClassDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateDeclaration()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"delegate void D();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"delegate void D();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedDelegateDeclaration()
        {
            VerifyAbsence(
@"class C {
    delegate void D();
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedMember()
        {
            VerifyAbsence(@"class A {
    class C {}
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNamespaceKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    namespace $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousNamespace_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousNamespace_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
   namespace N1 {}
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    $$
    using Foo;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMember_InsideNamespace()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"namespace N {
    class C {}
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMember_InsideNamespace_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"namespace N {
    class C {}
    $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeExtern()
        {
            VerifyAbsence(@"$$
extern alias Foo;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing()
        {
            VerifyAbsence(@"$$
using Foo;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBetweenUsings()
        {
            VerifyAbsence(AddInsideMethod(
@"using Foo;
$$
using Bar;"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalAttribute()
        {
            VerifyKeyword(SourceCodeKind.Regular,
@"[assembly: Foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalAttribute_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"[assembly: Foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAttribute()
        {
            VerifyAbsence(
@"[Foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedAttribute()
        {
            VerifyAbsence(
@"class C {
  [Foo]
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterRegion()
        {
            VerifyKeyword(SourceCodeKind.Regular,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterRegion_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
        @"#region EDM Relationship Metadata

[assembly: EdmRelationshipAttribute(""PerformanceResultsModel"", ""FK_Runs_Machines"", ""Machines"", System.Data.Metadata.Edm.RelationshipMultiplicity.One, typeof(PerformanceViewerSL.Web.Machine), ""Runs"", System.Data.Metadata.Edm.RelationshipMultiplicity.Many, typeof(PerformanceViewerSL.Web.Run), true)]

#endregion

$$");
        }
    }
}
