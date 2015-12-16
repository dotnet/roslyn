// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class OverrideKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
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
        public void NotInCompilationUnit()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterExtern()
        {
            VerifyAbsence(@"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsing()
        {
            VerifyAbsence(@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNamespace()
        {
            VerifyAbsence(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTypeDeclaration()
        {
            VerifyAbsence(@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateDeclaration()
        {
            VerifyAbsence(@"delegate void Foo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodInClass()
        {
            VerifyKeyword(
@"class C {
  void Foo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterFieldInClass()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertyInClass()
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
        public void NotAfterAssemblyAttribute()
        {
            VerifyAbsence(@"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterRootAttribute()
        {
            VerifyAbsence(@"[foo]
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
        public void NotAfterPartial()
        {
            VerifyAbsence(@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAbstract()
        {
            VerifyAbsence(@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInternal()
        {
            VerifyAbsence(@"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPublic()
        {
            VerifyAbsence(@"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStaticInternal()
        {
            VerifyAbsence(@"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInternalStatic()
        {
            VerifyAbsence(@"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInvalidInternal()
        {
            VerifyAbsence(@"virtual internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass()
        {
            VerifyAbsence(@"class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPrivate()
        {
            VerifyAbsence(@"private $$");
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
        public void NotAfterNestedStatic()
        {
            VerifyAbsence(@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedPrivate()
        {
            VerifyAbsence(@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedNew()
        {
            VerifyAbsence(
@"class C {
    new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegate()
        {
            VerifyAbsence(@"delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedVirtual()
        {
            VerifyAbsence(@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedOverride()
        {
            VerifyAbsence(@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInProperty()
        {
            VerifyAbsence(
@"class C {
    int Foo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterAccessor()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterAccessibility()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterInternal()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }
    }
}
