// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class SetKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { $$
            """);

    [Fact]
    public Task TestAfterPropertyPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { private $$
            """);

    [Fact]
    public Task TestAfterPropertyAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { [Bar] $$
            """);

    [Fact]
    public Task TestAfterPropertyAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { [Bar] private $$
            """);

    [Fact]
    public Task TestAfterPropertyGet()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get; $$
            """);

    [Fact]
    public Task TestAfterPropertyGetAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get; private $$
            """);

    [Fact]
    public Task TestAfterPropertyGetAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get; [Bar] $$
            """);

    [Fact]
    public Task TestAfterPropertyGetAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get; [Bar] private $$
            """);

    [Fact]
    public Task TestAfterGetAccessorBlock()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get { } $$
            """);

    [Fact]
    public Task TestAfterGetAccessorBlockAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get { } private $$
            """);

    [Fact]
    public Task TestAfterGetAccessorBlockAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get { } [Bar] $$
            """);

    [Fact]
    public Task TestAfterGetAccessorBlockAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int Goo { get { } [Bar] private $$
            """);

    [Fact]
    public Task TestNotAfterPropertySetKeyword()
        => VerifyAbsenceAsync(
            """
            class C {
               int Goo { set $$
            """);

    [Fact]
    public Task TestNotAfterPropertySetAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
               int Goo { set; $$
            """);

    [Fact]
    public Task TestNotInEvent()
        => VerifyAbsenceAsync(
            """
            class C {
               event Goo E { $$
            """);

    [Fact]
    public Task TestAfterIndexer()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { $$
            """);

    [Fact]
    public Task TestAfterIndexerPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { private $$
            """);

    [Fact]
    public Task TestAfterIndexerAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { [Bar] private $$
            """);

    [Fact]
    public Task TestAfterIndexerGet()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get; $$
            """);

    [Fact]
    public Task TestAfterIndexerGetAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get; private $$
            """);

    [Fact]
    public Task TestAfterIndexerGetAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get; [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerGetAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get; [Bar] private $$
            """);

    [Fact]
    public Task TestAfterIndexerGetBlock()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get { } $$
            """);

    [Fact]
    public Task TestAfterIndexerGetBlockAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get { } private $$
            """);

    [Fact]
    public Task TestAfterIndexerGetBlockAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get { } [Bar] $$
            """);

    [Fact]
    public Task TestAfterIndexerGetBlockAndAttributeAndPrivate()
        => VerifyKeywordAsync(
            """
            class C {
               int this[int i] { get { } [Bar] private $$
            """);

    [Fact]
    public Task TestNotAfterIndexerSetKeyword()
        => VerifyAbsenceAsync(
            """
            class C {
               int this[int i] { set $$
            """);

    [Fact]
    public Task TestNotAfterIndexerSetAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
               int this[int i] { set; $$
            """);
}
