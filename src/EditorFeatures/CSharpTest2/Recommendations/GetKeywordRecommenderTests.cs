// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class GetKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterPropertySet()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set; $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertySetAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set; private $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertySetAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set; [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertySetAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set; [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterSetAccessorBlock()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set { } $$
                """);
        }

        [Fact]
        public async Task TestAfterSetAccessorBlockAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set { } private $$
                """);
        }

        [Fact]
        public async Task TestAfterSetAccessorBlockAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set { } [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterSetAccessorBlockAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo { set { } [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPropertyGetKeyword()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int Goo { get $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPropertyGetAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int Goo { get; $$
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
        public async Task TestAfterIndexerSet()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set; $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set; private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set; [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set; [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetBlock()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set { } $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetBlockAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set { } private $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetBlockAndAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set { } [Bar] $$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerSetBlockAndAttributeAndPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { set { } [Bar] private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexerGetKeyword()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int this[int i] { get $$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexerGetAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   int this[int i] { get; $$
                """);
        }

        [Fact]
        public async Task TestBeforeSemicolon()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { $$; }
                """);
        }

        [Fact]
        public async Task TestAfterProtectedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { protected internal $$ }
                """);
        }

        [Fact]
        public async Task TestAfterInternalProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int this[int i] { internal protected $$ }
                """);
        }
    }
}
