// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class TypeKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestInOuterAttribute()
        => VerifyKeywordAsync(
@"[$$");

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInClassTypeParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

    [Fact]
    public Task TestNotInDelegateTypeParameters()
        => VerifyAbsenceAsync(
@"delegate void D<[$$");

    [Fact]
    public Task TestNotInMethodTypeParameters()
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
}
