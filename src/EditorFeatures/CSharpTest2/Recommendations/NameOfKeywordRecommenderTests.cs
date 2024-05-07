// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NameOfKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestOfferedInAttributeConstructorArgumentList()
            => await VerifyKeywordAsync("using System.ComponentModel; [DefaultValue($$ class C { }");

        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestConstMemberInitializer()
        {
            await VerifyKeywordAsync(
                """
                class E {
                    const string a = $$
                }
                """);
        }

        [Fact]
        public async Task TestInMemberInitializer1()
        {
            await VerifyKeywordAsync(
                """
                class E {
                    int a = $$
                }
                """);
        }

        [Fact]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [Fact]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
