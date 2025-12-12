// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public abstract class NativeIntegerKeywordRecommenderTests : RecommenderTests
{
    private protected abstract AbstractNativeIntegerKeywordRecommender Recommender { get; }

    protected NativeIntegerKeywordRecommenderTests()
    {
        RecommendKeywordsAsync = (position, context) => Task.FromResult(Recommender.RecommendKeywords(position, context, CancellationToken.None));
    }

    [Fact]
    public Task TestInLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInClass()
        => VerifyKeywordAsync(
            """
            class C
            {
                $$
            """);

    [Fact]
    public Task TestInParameterList()
        => VerifyKeywordAsync(
            """
            class C
            {
                void F($$
            """);

    [Fact]
    public Task TestInLambdaParameterListFirst()
        => VerifyKeywordAsync(AddInsideMethod(
@"F(($$"));

    [Fact]
    public Task TestInLambdaParameterListLater()
        => VerifyKeywordAsync(AddInsideMethod(
@"F((int x, $$"));

    [Fact]
    public Task TestAfterConst()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$"));

    [Fact]
    public Task TestInFixedStatement()
        => VerifyKeywordAsync(
@"fixed ($$");

    [Fact]
    public Task TestInRef()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));

    [Fact]
    public Task TestInMemberType()
        => VerifyKeywordAsync(
            """
            class C
            {
                private $$
            """);

    [Fact]
    public Task TestInOperatorType()
        => VerifyKeywordAsync(
            """
            class C
            {
                static implicit operator $$
            """);

    [Fact]
    public Task TestInEnumUnderlyingType()
        => VerifyAbsenceAsync(
@"enum E : $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotInTypeParameterConstraint_TypeDeclaration1()
        => VerifyAbsenceAsync(
@"class C<T> where T : $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestInTypeParameterConstraint_TypeDeclaration_WhenNotDirectlyInConstraint1()
        => VerifyKeywordAsync(
@"class C<T> where T : IList<$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotInTypeParameterConstraint_TypeDeclaration2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                    where T : $$
                    where U : U
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestInTypeParameterConstraint_TypeDeclaration_WhenNotDirectlyInConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                    where T : IList<$$
                    where U : U
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotInTypeParameterConstraint_MethodDeclaration1()
        => VerifyAbsenceAsync(
            """
            class C
            {
                public void M<T>() where T : $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestInTypeParameterConstraint_MethodDeclaration_WhenNotDirectlyInConstraint1()
        => VerifyKeywordAsync(
            """
            class C
            {
                public void M<T>() where T : IList<$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotInTypeParameterConstraint_MethodDeclaration2()
        => VerifyAbsenceAsync(
            """
            class C
            {
                public void M<T>()
                    where T : $$
                    where U : T
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotInTypeParameterConstraint_MethodDeclaration_WhenNotDirectlyInConstraint2()
        => VerifyKeywordAsync(
            """
            class C
            {
                public void M<T>()
                    where T : IList<$$
                    where U : T
            """);

    [Fact]
    public Task TestInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = 1 + $$"));

    [Fact]
    public Task TestInDefault()
        => VerifyKeywordAsync(AddInsideMethod(
@"_ = default($$"));

    [Fact]
    public Task TestInCastType()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = (($$"));

    [Fact]
    public Task TestInNew()
        => VerifyKeywordAsync(AddInsideMethod(
@"_ = new $$"));

    [Fact]
    public Task TestAfterAs()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            object x = null;
            var y = x as $$
            """));

    [Fact]
    public Task TestAfterIs()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            object x = null;
            if (x is $$
            """));

    [Fact]
    public Task TestAfterStackAlloc()
        => VerifyKeywordAsync(
            """
            class C
            {
                nint* p = stackalloc $$
            """);

    [Fact]
    public Task TestNotInUsing()
        => VerifyAbsenceAsync(
@"using $$");

    [Fact]
    public Task TestInUsingAliasFirst()
        => VerifyKeywordAsync(
@"using A = $$");

    [Fact]
    public Task TestInGlobalUsingAliasFirst()
        => VerifyKeywordAsync(
@"global using A = $$");

    [Fact]
    public Task TestInUsingAliasLater()
        => VerifyKeywordAsync(
@"using A = List<$$");

    [Fact]
    public Task TestInGlobalUsingAliasLater()
        => VerifyKeywordAsync(
@"global using A = List<$$");

    [Fact]
    public Task TestInNameOf()
        => VerifyKeywordAsync(AddInsideMethod(
@"_ = nameof($$"));

    [Fact]
    public Task TestInSizeOf()
        => VerifyKeywordAsync(AddInsideMethod(
@"_ = sizeof($$"));

    [Fact]
    public Task TestInCRef()
        => VerifyAbsenceAsync(AddInsideMethod(
@"/// <see cref=""$$"));

    [Fact]
    public Task TestInTupleWithinType()
        => VerifyKeywordAsync("""
            class Program
            {
                ($$
            }
            """);

    [Fact]
    public Task TestInTupleWithinMember()
        => VerifyKeywordAsync("""
            class Program
            {
                void Method()
                {
                    ($$
                }
            }
            """);

    [Fact]
    public Task TestPatternInSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch(o)
            {
                case $$
            }
            """));

    [Fact]
    public Task TestInFunctionPointerType()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterComma()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterModifier()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task TestNotAfterAsync()
        => VerifyAbsenceAsync("""
            class C
            {
                async $$
            """);

    [Fact]
    public Task TestNotAfterDelegateAsterisk()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*$$
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync))]
    public Task TestAfterKeywordIndicatingLocalFunctionWithoutAsync(string keyword)
        => VerifyKeywordAsync(AddInsideMethod($"""
            {keyword} $$
            """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithAsync))]
    public Task TestNotAfterKeywordIndicatingLocalFunctionWithAsync(string keyword)
        => VerifyAbsenceAsync(AddInsideMethod($"""
            {keyword} $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49743")]
    public Task TestNotInPreprocessorDirective()
        => VerifyAbsenceAsync(
@"#$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64585")]
    public Task TestAfterRequired()
        => VerifyKeywordAsync("""
            class C
            {
                required $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70074")]
    public Task TestNotInAttribute1()
        => VerifyAbsenceAsync("""
            [$$
            class C
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70074")]
    public Task TestNotInAttribute2()
        => VerifyAbsenceAsync("""
            class C
            {
                [$$
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70074")]
    public Task TestNotInAttribute3()
        => VerifyAbsenceAsync("""
            class C
            {
                void M()
                {
                    [$$
                    void L()
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestNotInRecordParameterAttribute()
        => VerifyAbsenceAsync(
            """
            record R([$$] int i) { }
            """);

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

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
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
}
