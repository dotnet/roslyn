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
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
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
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
   int Goo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyGet()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPropertySetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Goo { set $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPropertySetAccessor()
        {
            VerifyAbsence(
@"class C {
   int Goo { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEvent()
        {
            VerifyAbsence(
@"class C {
   event Goo E { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexer()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerGetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexerSetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexerSetAccessor()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set; $$");
        }
    }
}
