// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ParamsKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotAfterAngle()
        => VerifyAbsenceAsync(
@"interface IGoo<$$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterIn()
        => VerifyAbsenceAsync(
@"interface IGoo<in $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAngle()
        => VerifyAbsenceAsync(
@"delegate void D<$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
@"delegate void D<Goo, $$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");

    [Fact]
    public Task TestNotParamsBaseListAfterAngle()
        => VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");

    [Fact]
    public Task TestNotInGenericMethod()
        => VerifyAbsenceAsync(
            """
            interface IGoo {
                void Goo<$$
            """);

    [Fact]
    public Task TestNotAfterParams()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(ref $$
            """);

    [Fact]
    public Task TestNotAfterOut()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(out $$
            """);

    [Fact]
    public Task TestNotAfterThis()
        => VerifyAbsenceAsync(
            """
            static class C {
                static void Goo(this $$
            """);

    [Fact]
    public Task TestAfterMethodOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo($$
            """);

    [Fact]
    public Task TestAfterMethodComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, $$
            """);

    [Fact]
    public Task TestAfterMethodAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, [Goo]$$
            """);

    [Fact]
    public Task TestAfterConstructorOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                public C($$
            """);

    [Fact]
    public Task TestAfterConstructorComma()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, $$
            """);

    [Fact]
    public Task TestAfterConstructorAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, [Goo]$$
            """);

    [Fact]
    public Task TestAfterDelegateOpenParen()
        => VerifyKeywordAsync(
@"delegate void D($$");

    [Fact]
    public Task TestAfterDelegateComma()
        => VerifyKeywordAsync(
@"delegate void D(int i, $$");

    [Fact]
    public Task TestAfterDelegateAttribute()
        => VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");

    [Fact]
    public Task TestAfterLambdaOpenParen()
        => VerifyKeywordAsync(
@"var lam = ($$");

    [Fact]
    public Task TestAfterLambdaComma()
        => VerifyKeywordAsync(
@"var lam = (int i, $$");

    [Fact]
    public Task TestNotAfterOperator()
        => VerifyAbsenceAsync(
            """
            class C {
                static int operator +($$
            """);

    [Fact]
    public Task TestNotAfterDestructor()
        => VerifyAbsenceAsync(
            """
            class C {
                ~C($$
            """);

    [Fact]
    public Task TestAfterIndexer()
        => VerifyKeywordAsync(
            """
            class C {
                int this[$$
            """);

    [Fact]
    public Task TestNotInObjectCreationAfterOpenParen()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar($$
            """);

    [Fact]
    public Task TestNotAfterParamsParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(ref $$
            """);

    [Fact]
    public Task TestNotAfterOutParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(out $$
            """);

    [Fact]
    public Task TestNotInObjectCreationAfterComma()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz, $$
            """);

    [Fact]
    public Task TestNotInObjectCreationAfterSecondComma()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz, quux, $$
            """);

    [Fact]
    public Task TestNotInObjectCreationAfterSecondNamedParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz: 4, quux: $$
            """);

    [Fact]
    public Task TestNotInInvocationExpression()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  Bar($$
            """);

    [Fact]
    public Task TestNotInInvocationAfterComma()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  Bar(baz, $$
            """);

    [Fact]
    public Task TestNotInInvocationAfterSecondComma()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  Bar(baz, quux, $$
            """);

    [Fact]
    public Task TestNotInInvocationAfterSecondNamedParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  Bar(baz: 4, quux: $$
            """);
}
