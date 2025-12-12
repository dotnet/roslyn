// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class BaseKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotInTopLevelMethod()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            void Goo()
            {
                $$
            }
            """);

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement()
        => VerifyAbsenceAsync(
            """
            System.Console.WriteLine();
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration()
        => VerifyAbsenceAsync(
            """
            int i = 0;
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestInClassConstructorInitializer()
        => VerifyKeywordAsync(
            """
            class C {
                public C() : $$
            """);

    [Fact]
    public Task TestInRecordConstructorInitializer()
        => VerifyWorkerAsync("""
            record C {
                public C() : $$
            """, absent: false, options: TestOptions.RegularPreview);

    [Fact]
    public Task TestNotInStaticClassConstructorInitializer()
        => VerifyAbsenceAsync(
            """
            class C {
                static C() : $$
            """);

    [Fact]
    public Task TestNotInStructConstructorInitializer()
        => VerifyAbsenceAsync(
            """
            struct C {
                public C() : $$
            """);

    [Fact]
    public Task TestAfterCast()
        => VerifyKeywordAsync(
            """
            struct C {
                new internal ErrorCode Code { get { return (ErrorCode)$$
            """);

    [Fact]
    public Task TestInEmptyMethod()
        => VerifyKeywordAsync(
            SourceCodeKind.Regular,
            AddInsideMethod(
@"$$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInEnumMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            enum E {
                a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/16335")]
    public Task InExpressionBodyAccessor()
        => VerifyKeywordAsync("""
            class B
            {
                public virtual int T { get => bas$$ }
            }
            """);

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion
}
