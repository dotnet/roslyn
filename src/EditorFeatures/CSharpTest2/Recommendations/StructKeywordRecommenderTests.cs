// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class StructKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCompilationUnit()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern()
        {
            VerifyKeyword(
@"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsing()
        {
            VerifyKeyword(
@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNamespace()
        {
            VerifyKeyword(
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTypeDeclaration()
        {
            VerifyKeyword(
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateDeclaration()
        {
            VerifyKeyword(
@"delegate void Foo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Foo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Foo;");
        }

        [WpfFact(Skip = "528041"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterAssemblyAttribute()
        {
            VerifyKeyword(
@"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterRootAttribute()
        {
            VerifyKeyword(
@"[foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [foo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInsideInterface()
        {
            VerifyAbsence(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPartial()
        {
            VerifyKeyword(
@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAbstract()
        {
            VerifyAbsence(@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterInternal()
        {
            VerifyKeyword(
@"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPublic()
        {
            VerifyKeyword(
@"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPrivate()
        {
            VerifyKeyword(
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterProtected()
        {
            VerifyKeyword(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterSealed()
        {
            VerifyAbsence(@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStatic()
        {
            VerifyAbsence(@"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAbstractPublic()
        {
            VerifyAbsence(@"abstract public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStruct()
        {
            VerifyAbsence(@"struct $$");
        }
    }
}
