// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class PropertyKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
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

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
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
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

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
        => VerifyAbsenceAsync(
@"[$$");

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute1()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute2()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get { } [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute1()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute2()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { add { } [$$
            """);

    [Fact]
    public Task TestNotInTypeParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51756")]
    public Task TestInRecordPositionalParameter1()
        => VerifyKeywordAsync("public record R([$$] string M);");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51756")]
    public Task TestInRecordPositionalParameter2()
        => VerifyKeywordAsync("public record R([$$ SomeAttribute] string M);");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51756")]
    public Task TestInRecordPositionalParameter3()
        => VerifyKeywordAsync("public record R([$$ string M);");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51756")]
    public Task TestInRecordPositionalParameter4()
        => VerifyKeywordAsync("public record R([$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestInRecordParameterAttribute()
        => VerifyKeywordAsync(
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
                """, CSharpNextParseOptions);

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
