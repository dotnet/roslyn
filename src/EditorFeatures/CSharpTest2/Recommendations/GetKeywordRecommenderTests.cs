// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class GetKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterPropertySet()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertySetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertySetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertySetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPropertyGetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Goo { get $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPropertyGetAccessor()
        {
            VerifyAbsence(
@"class C {
   int Goo { get; $$");
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
        public async Task TestAfterIndexerSet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } [Bar] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexerSetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set { } [Bar] private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexerGetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { get $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexerGetAccessor()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeSemicolon()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProtectedInternal()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { protected internal $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternalProtected()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { internal protected $$ }");
        }
    }
}
