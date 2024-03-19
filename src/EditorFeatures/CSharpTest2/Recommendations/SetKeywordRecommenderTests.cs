// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class SetKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { private $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyGet()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get; $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyGetAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get; private $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyGetAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get; [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyGetAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get; [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterGetAccessorBlock()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get { } $$
                """);
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get { } private $$
                """);
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get { } [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterGetAccessorBlockAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { get { } [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPropertySetKeyword()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int Goo { set $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPropertySetAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int Goo { set; $$
                """);
        }

        [Fact]
        public async Task TestNotInEvent()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   event Goo E { $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGet()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get; $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get; private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get; [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get; [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetBlock()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get { } $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get { } private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get { } [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerGetBlockAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { get { } [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexerSetKeyword()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int this[int i] { set $$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexerSetAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int this[int i] { set; $$
                """);
        }
    }
}
