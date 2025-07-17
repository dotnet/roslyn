// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ThrowKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

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
    public Task TestAfterIf()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) 
                $$
            """));

    [Fact]
    public Task TestAfterDo()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            do 
                $$
            """));

    [Fact]
    public Task TestAfterWhile()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            while (true) 
                $$
            """));

    [Fact]
    public Task TestAfterFor()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (int i = 0; i < 10; i++) 
                $$
            """));

    [Fact]
    public Task TestAfterForeach()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in bar)
                $$
            """));

    [Fact]
    public Task TestNotAfterThrow()
        => VerifyAbsenceAsync(AddInsideMethod(
@"throw $$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Fact]
    public Task TestInNestedIf()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (caseOrDefaultKeywordOpt != null) {
                if (caseOrDefaultKeyword.Kind != SyntaxKind.CaseKeyword && caseOrDefaultKeyword.Kind != SyntaxKind.DefaultKeyword) 
                  $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
    public Task TestAfterArrow()
        => VerifyKeywordAsync(
            """
            class C
            {
                void Goo() => $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
    public Task TestAfterQuestionQuestion()
        => VerifyKeywordAsync(
            """
            class C
            {
                public C(object o)
                {
                    _o = o ?? $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
    public Task TestInConditional1()
        => VerifyKeywordAsync(
            """
            class C
            {
                public C(object o)
                {
                    var v= true ? $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
    public Task TestInConditional2()
        => VerifyKeywordAsync(
            """
            class C
            {
                public C(object o)
                {
                    var v= true ? 0 : $$
            """);
}
