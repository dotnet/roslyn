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
    public class ReturnKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
        public async Task TestAtRoot_Regular()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
        public async Task TestAfterClass_Regular()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
        public async Task TestAfterGlobalStatement_Regular()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
        public async Task TestAfterGlobalVariableDeclaration_Regular()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
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
        public async Task TestIncompleteStatementAttributeList()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[$$"));
        }

        [Fact]
        public async Task TestStatementAttributeList()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[$$Attr]"));
        }

        [Fact]
        public async Task TestLocalFunctionAttributeList()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[$$Attr] void local1() { }"));
        }

        [Fact]
        public async Task TestNotInLocalFunctionParameterAttributeList()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"void local1([$$Attr] int i) { }"));
        }

        [Fact]
        public async Task TestNotInLocalFunctionTypeParameterAttributeList()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"void local1<[$$Attr] T>() { }"));
        }

        [Fact]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterAwait()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    async void M()
                    {
                        await $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                $$
                return true;
                """));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return true;
                $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestNotAfterReturn()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"return $$"));
        }

        [Fact]
        public async Task TestAfterYield()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield $$"));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                  $$
                }
                """);
        }

        [Fact]
        public async Task TestInAttributeInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    [$$
                """);
        }

        [Fact]
        public async Task TestInAttributeAfterAttributeInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    [Goo]
                    [$$
                """);
        }

        [Fact]
        public async Task TestInAttributeAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                    }
                    [$$
                """);
        }

        [Fact]
        public async Task TestInAttributeAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int Goo {
                        get;
                    }
                    [$$
                """);
        }

        [Fact]
        public async Task TestInAttributeAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int Goo;
                    [$$
                """);
        }

        [Fact]
        public async Task TestInAttributeAfterEvent()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    event Action<int> Goo;
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInOuterAttribute()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"[$$");
        }

        [Fact]
        public async Task TestInOuterAttributeScripting()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"[$$");
        }

        [Fact]
        public async Task TestNotInParameterAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo([$$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { [$$
                """);
        }

        [Fact]
        public async Task TestNotInEventAttribute()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event Action<int> Goo { [$$
                """);
        }

        [Fact]
        public async Task TestNotInClassReturnParameters()
        {
            await VerifyAbsenceAsync(
@"class C<[$$");
        }

        [Fact]
        public async Task TestNotInDelegateReturnParameters()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[$$");
        }

        [Fact]
        public async Task TestNotInMethodReturnParameters()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void M<[$$
                """);
        }

        [Fact]
        public async Task TestInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                    [$$
                """);
        }

        [Fact]
        public async Task TestInStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                    [$$
                """);
        }

        [Fact]
        public async Task TestNotInEnum()
        {
            await VerifyAbsenceAsync(
                """
                enum E {
                    [$$
                """);
        }

        [Fact]
        public async Task TestAfterElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (goo) {
                } else $$
                """));
        }

        [Fact]
        public async Task TestAfterElseClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (goo) {
                } else {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestAfterFixed()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                fixed (byte* pResult = result) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestAfterSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (goo) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestAfterCatch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } catch {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestAfterFinally()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } finally {
                }
                $$
                """));
        }
    }
}
