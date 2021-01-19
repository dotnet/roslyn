// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class SetKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
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
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
   int Goo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyGet()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPropertyGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPropertySetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Goo { set $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPropertySetAccessor()
        {
            VerifyAbsence(
@"class C {
   int Goo { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEvent()
        {
            VerifyAbsence(
@"class C {
   event Goo E { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexer()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIndexerGetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIndexerSetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIndexerSetAccessor()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set; $$");
        }
    }
}
