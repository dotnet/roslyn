// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ParamKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529127")]
    public async Task TestNotOfferedInsideArgumentList()
        => await VerifyAbsenceAsync("class C { void M([$$");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529127")]
    public async Task TestNotOfferedInsideArgumentList2()
        => await VerifyAbsenceAsync("delegate void M([$$");

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
    public Task TestNotInAttributeInsideClass()
        => VerifyAbsenceAsync(
            """
            class C {
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterAttributeInsideClass()
        => VerifyAbsenceAsync(
            """
            class C {
                [Goo]
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterProperty()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterField()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo;
                [$$
            """);

    [Fact]
    public Task TestNotInAttributeAfterEvent()
        => VerifyAbsenceAsync(
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
    public Task TestInPropertyAttribute1()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestInPropertyAttribute2()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { get { } [$$
            """);

    [Fact]
    public Task TestInEventAttribute1()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestInEventAttribute2()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo { add { } [$$
            """);

    [Fact]
    public Task TestNotInTypeParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

    [Fact]
    public Task TestNotInInterface()
        => VerifyAbsenceAsync(
            """
            interface I {
                [$$
            """);

    [Fact]
    public Task TestNotInStruct()
        => VerifyAbsenceAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestInRecordParameterAttribute()
        => VerifyKeywordAsync(
            """
            record R([$$] int i) { }
            """);
}
