// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class InitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
   int Goo { $$");
        }

        [Fact]
        public async Task TestAfterPropertyPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { private $$");
        }

        [Fact]
        public async Task TestAfterPropertyAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] $$");
        }

        [Fact]
        public async Task TestAfterPropertyAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] private $$");
        }

        [Fact]
        public async Task TestAfterPropertyGet()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; $$");
        }

        [Fact]
        public async Task TestAfterPropertyGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; private $$");
        }

        [Fact]
        public async Task TestAfterPropertyGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] $$");
        }

        [Fact]
        public async Task TestAfterPropertyGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] private $$");
        }

        [Fact]
        public async Task TestAfterGetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } $$");
        }

        [Fact]
        public async Task TestAfterSetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } $$");
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } private $$");
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] $$");
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] private $$");
        }

        [Fact]
        public async Task TestNotAfterPropertySetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Goo { set $$");
        }

        [Fact]
        public async Task TestAfterPropertySetAccessor()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; $$");
        }

        [Fact]
        public async Task TestNotInEvent()
        {
            VerifyAbsence(
@"class C {
   event Goo E { $$");
        }

        [Fact]
        public async Task TestAfterIndexer()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$");
        }

        [Fact]
        public async Task TestAfterIndexerPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { private $$");
        }

        [Fact]
        public async Task TestAfterIndexerAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] $$");
        }

        [Fact]
        public async Task TestAfterIndexerAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] private $$");
        }

        [Fact]
        public async Task TestAfterIndexerGet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; private $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] private $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } private $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] $$");
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] private $$");
        }

        [Fact]
        public async Task TestNotAfterIndexerSetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set $$");
        }

        [Fact]
        public async Task TestAfterIndexerSetAccessor()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; $$");
        }
    }
}
