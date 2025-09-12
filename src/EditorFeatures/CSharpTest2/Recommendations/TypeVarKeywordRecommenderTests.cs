// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class TypeVarKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestInClassTypeParameters()
        => VerifyKeywordAsync(
@"class C<[$$");

    [Fact]
    public Task TestInDelegateTypeParameters()
        => VerifyKeywordAsync(
@"delegate void D<[$$");

    [Fact]
    public Task TestInMethodTypeParameters()
        => VerifyKeywordAsync(
            """
            class C {
                void M<[$$
            """);

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
}
