// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ReturnKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
    public Task TestAtRoot_Regular()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
    public Task TestAfterClass_Regular()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
    public Task TestAfterGlobalStatement_Regular()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57121")]
    public Task TestAfterGlobalVariableDeclaration_Regular()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestIncompleteStatementAttributeList()
        => VerifyKeywordAsync(AddInsideMethod(
@"[$$"));

    [Fact]
    public Task TestStatementAttributeList()
        => VerifyKeywordAsync(AddInsideMethod(
@"[$$Attr]"));

    [Fact]
    public Task TestLocalFunctionAttributeList()
        => VerifyKeywordAsync(AddInsideMethod(
@"[$$Attr] void local1() { }"));

    [Fact]
    public Task TestNotInLocalFunctionParameterAttributeList()
        => VerifyAbsenceAsync(AddInsideMethod(
@"void local1([$$Attr] int i) { }"));

    [Fact]
    public Task TestNotInLocalFunctionTypeParameterAttributeList()
        => VerifyAbsenceAsync(AddInsideMethod(
@"void local1<[$$Attr] T>() { }"));

    [Fact]
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAfterAwait()
        => VerifyAbsenceAsync(
            """
            class C
            {
                async void M()
                {
                    await $$
                }
            }
            """);

    [Fact]
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestNotAfterReturn()
        => VerifyAbsenceAsync(AddInsideMethod(
@"return $$"));

    [Fact]
    public Task TestAfterYield()
        => VerifyKeywordAsync(AddInsideMethod(
@"yield $$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Fact]
    public Task TestInAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [Goo]
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterField()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo;
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterEvent()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);

    [Fact]
    public Task TestNotInOuterAttribute()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"[$$");

    [Fact]
    public Task TestInOuterAttributeScripting()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"[$$");

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71200")]
    public Task TestInPropertyAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71200")]
    public Task TestInEventAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInClassReturnParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

    [Fact]
    public Task TestNotInDelegateReturnParameters()
        => VerifyAbsenceAsync(
@"delegate void D<[$$");

    [Fact]
    public Task TestNotInMethodReturnParameters()
        => VerifyAbsenceAsync(
            """
            class C {
                void M<[$$
            """);

    [Fact]
    public Task TestInInterface()
        => VerifyKeywordAsync(
            """
            interface I {
                [$$
            """);

    [Fact]
    public Task TestInStruct()
        => VerifyKeywordAsync(
            """
            struct S {
                [$$
            """);

    [Fact]
    public Task TestNotInEnum()
        => VerifyAbsenceAsync(
            """
            enum E {
                [$$
            """);

    [Fact]
    public Task TestAfterElse()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (goo) {
            } else $$
            """));

    [Fact]
    public Task TestAfterElseClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (goo) {
            } else {
            }
            $$
            """));

    [Fact]
    public Task TestAfterFixed()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            fixed (byte* pResult = result) {
            }
            $$
            """));

    [Fact]
    public Task TestAfterSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (goo) {
            }
            $$
            """));

    [Fact]
    public Task TestAfterCatch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            try {
            } catch {
            }
            $$
            """));

    [Fact]
    public Task TestAfterFinally()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            try {
            } finally {
            }
            $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestNotInRecordParameterAttribute()
        => VerifyAbsenceAsync(
            """
            record R([$$] int i) { }
            """);

    [Fact]
    public Task TestWithinExtension1()
        => VerifyAbsenceAsync(
            """
                static class C
                {
                    extension(string s)
                    {
                        $$
                    }
                }
                """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);

    [Fact]
    public Task TestWithinExtension2()
        => VerifyKeywordAsync(
            """
                static class C
                {
                    extension(string s)
                    {
                        [$$
                    }
                }
                """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
