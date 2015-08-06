// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class GetKeywordRecommenderTests : KeywordRecommenderTests
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
        public void AfterProperty()
        {
            VerifyKeyword(
@"class C {
   int Foo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertyPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertyAttribute()
        {
            VerifyKeyword(
@"class C {
   int Foo { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertyAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertySet()
        {
            VerifyKeyword(
@"class C {
   int Foo { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertySetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { set; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertySetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Foo { set; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertySetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { set; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Foo { set { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { set { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Foo { set { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Foo { set { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPropertyGetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Foo { get $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPropertyGetAccessor()
        {
            VerifyAbsence(
@"class C {
   int Foo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEvent()
        {
            VerifyAbsence(
@"class C {
   event Foo E { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexer()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexerSetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIndexerGetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { get $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIndexerGetAccessor()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeSemicolon()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterProtectedInternal()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { protected internal $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterInternalProtected()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { internal protected $$ }");
        }
    }
}
